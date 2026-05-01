from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from langchain_text_splitters import RecursiveCharacterTextSplitter
import pdfplumber
import chromadb
import os
import io

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
)

# ==========================================
# KHỞI TẠO VECTOR DATABASE (CHROMADB)
# ==========================================

DB_PATH = "./data/chroma_db_data"
try:
    chroma_client = chromadb.PersistentClient(path=DB_PATH)
    print(f"Đã kết nốt thành công đến ChromaDB tại: {DB_PATH}")
except Exception as e:
    print(f"Lỗi khi khởi tạo ChromaBD: {e}")

#Tạo 1 collection trong ChromaDB (tương tạo 1 bảng trong SQL)
collection = chroma_client.get_or_create_collection(
    name="enterprise_knowledge",
    metadata={"hnsw:space":"cosine"}
)

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


