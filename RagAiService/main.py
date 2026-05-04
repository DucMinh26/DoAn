from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from langchain_text_splitters.character import RecursiveCharacterTextSplitter
import pdfplumber
import chromadb
import google.generativeai as genai 
from dotenv import load_dotenv
import os
import io
from pydantic import BaseModel
from typing import Optional


load_dotenv()
genai.configure(api_key=os.getenv("GOOGLE_API_KEY"))

#Khởi tạo ứng dụng FastAPI
app = FastAPI(
    title="AI RAG Engine",
    description="icroservice xử lý tài liệu và AI cho hệ thống PMIS",
    version="1.0.0"
)

#Cấu hình CORS cho phép .NET hoặc web gọi API vào python
app.add_middleware( 
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
    expose_headers=["*"],
)

# ==========================================
# CẤU HÌNH CHROMADB
# ==========================================


DB_PATH = "F:/DoAn/RagAiService/data/chromaDB"
try:
    chroma_client = chromadb.PersistentClient(path=DB_PATH)

    collection = chroma_client.get_or_create_collection(
        name="enterprise_knowledge",
        embedding_function=None,
    )

except Exception as e:
    print(f"[CHROMA][ERROR] Lỗi khi khởi tạo ChromaDB: {type(e).__name__}: {e}")

# ==========================================
# CÁC CLASS ĐỊNH DẠNG DỮ LIỆU (PYDANTIC MODELS)
# ==========================================

class SearchQuery(BaseModel): #khai báo lớp SearchQuery kế thừa BaseModel
    query: str
    document_id: Optional[str] =None #khai báo id là str còn nếu để trống thì là None
    top_k: int=3
# ==========================================
# CÁC HÀM HỖ TRỢ (HELPER FUNCTIONS)
# ==========================================
def extract_text_from_bytes(file_bytes: bytes) -> str:
    """Hàm hỗ trợ: Đọc file PDF từ byte và trả về chuỗi văn bản"""
    text_content= []
    with pdfplumber.open(io.BytesIO(file_bytes)) as pdf:
        for page in pdf.pages:
            page_text = page.extract_text()
            if page_text:
                text_content.append(page_text)

    return "\n".join(text_content)

def chunk_text(text: str) ->list[str]:
    text_splitter = RecursiveCharacterTextSplitter(
        chunk_size=1000,
        chunk_overlap =200,
        length_function=len,
        separators=['\n\n','\n','.',' ','']
    )
    chunks = text_splitter.split_text(text)
    return chunks

# ==========================================
# CÁC API ENDPOINTS
# ==========================================

@app.get("/")
def read_root():
    return{
        "status": "success",
        "message": "AI Engine đang hoạt động",
        "version": "1.0.0"
    }

@app.post("/api/ingest-test")
async def test_ingestion_pipeline(file: UploadFile = File(...)):
    if not file.filename.endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Chỉ hỗ trợ định dạng PDF")

    try:
        file_bytes = await file.read()

        raw_text = extract_text_from_bytes(file_bytes)

        if not raw_text.strip():
            return {"status": "warning", "message": "Không tìm thấy chữ trong PDF."}
        
        document_chunks = chunk_text(raw_text)

        return{
            "status":"success",
            "filename":file.filename,
            "total_characters":len(raw_text),
            "total_chunk":len(document_chunks),
            "sample_chunk_1": document_chunks[0] if len(document_chunks)>0 else "",
            "sample_chunk_2": document_chunks[1] if len(document_chunks)>1 else ""
        }
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi khi xử lý: {str(e)}")

@app.post("/api/ingest")
async def ingest_document(
    file: UploadFile = File(...),
    document_id: str = Form(...)
):
    if not file.filename.endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Chỉ hỗ trợ định dạng PDF")

    try:
        file_bytes = await file.read()
        raw_text = extract_text_from_bytes(file_bytes)

        if not raw_text.strip():
            raise HTTPException(status_code=400, detail="PDF không có chữ")
        
        document_chunks = chunk_text(raw_text)

        #Tạo các cột trong 1 bảng
        documents_list = []
        metadatas_list = []
        ids_list = []
        embeddings_list = []

        print(f"Bắt đầu lấy Embedding từ Gemini cho {len(document_chunks)} đoạn...")

        for i, chunk in enumerate(document_chunks):
            print(f"Đang xử lý đoạn {i+1}/{len(document_chunks)}...")

            result = genai.embed_content(
                model="models/gemini-embedding-001",
                content=chunk,
                task_type="retrieval_document",
            )

            embeddings_list.append(result['embedding'])
            documents_list.append(chunk)
            metadatas_list.append({"document_id": document_id, "filename": file.filename})
            ids_list.append(f"doc_{document_id}_chunk_{i}")

        

        print(f"Đang tạo vector cho {len(documents_list)} đoạn văn...")
        # vectors = get_google_embeddings(document_list)
        print("Đã tạo vector xong, đang lưu vào DB...")

        collection.add(
            documents=documents_list,
            metadatas=metadatas_list,
            ids=ids_list,
            embeddings=embeddings_list
        )
        print("✅ Đã lưu xong vào ChromaDB!")
        
        return{
            "status":"success",
            "message":"Đã lưu tài liệu vào bộ não AI thành công",
            "document_id":document_id,
            "total_chunk_saved":len(document_chunks)
        }
    except Exception as e:
        print(f"Lỗi: {str(e)}") # In ra terminal để dễ debug
        raise HTTPException(status_code=500, detail=f"Lỗi hệ thống: {str(e)}")
    
