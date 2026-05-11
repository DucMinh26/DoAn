from dotenv import load_dotenv
import google.generativeai as genai
import os

# Thay 'YOUR_API_KEY' bằng API Key của Minh 
# Hoặc thiết lập biến môi trường để bảo mật hơn
load_dotenv()
genai.configure(api_key=os.getenv("GOOGLE_API_KEY"))

print("Danh sách các phiên bản Gemini hỗ trợ:")
print("-" * 50)

# Duyệt qua danh sách các model có hỗ trợ phương thức 'generateContent'
for m in genai.list_models():
    if 'generateContent' in m.supported_generation_methods:
        print(f"Tên Model: {m.name}")
        print(f"Mô tả: {m.description}")
        print("-" * 50)