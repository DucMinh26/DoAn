from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import pdfplumber
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

@app.get("/")
def read_root():
    return{
        "status": "success",
        "message": "AI Engine đang hoạt động",
        "version": "1.0.0"
    }

@app.post("/api/extract-text")
async def extract_text_from_pdf(file: UploadFile = File(...)):
    #1. Kiểm tra định dạng file
    if not file.filename.endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Chỉ hỗ trợ định dạng file PDF");

    text_content =""

    try:
        #2. Đọc file từ bộ nhớ tạm thành luồng byte
        file_bytes = await file.read()

        #3.Mở file pdf bằng pdfplumber
        with pdfplumber.open(io.BytesIO(file_bytes)) as pdf:
            for page in pdf.pages:
                page_text = page.extract_text()

                if page_text:
                    text_content += page_text + "\n"
        
        #4.Kiểm tra xem file có bị rỗng hay file chỉ chứa ảnh 
        if not text_content.strip():
            return {
                "status":"warning",
                "message":"Không tìm thấy chữ trong file PDF (có thể là ảnh chụp)",
                "text":""
            }
        
        return{
            "status": "success",
            "filename":file.filename,
            "total_pages":len(pdf.pages),
            "text_review": text_content[:200] +"..."
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Lỗi khi xử lý file PDF: {str(e)}")

