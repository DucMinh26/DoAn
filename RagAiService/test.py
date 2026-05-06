from dotenv import load_dotenv
import os
import google.generativeai as genai 
load_dotenv()
genai.configure(api_key=os.getenv("GOOGLE_API_KEY"))
print([m.name for m in genai.list_models() if 'embed' in m.supported_generation_methods or 'generateContent' in m.supported_generation_methods])