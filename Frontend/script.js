const API_BASE_URL = "http://localhost:5288";

const txtUsername = document.getElementById('username');
const txtPassword = document.getElementById('password');
const btnLogin = document.getElementById('btnLogin');
const authSection = document.getElementById('authSection');
const uploadSection = document.getElementById('uploadSection')

btnLogin.addEventListener('click', async() =>{
    const username = txtUsername.value.trim();
    const password = txtPassword.value.trim();

    if(!username || !password){
        alert("Vui lòng nhập tên đăng nhập và mật khẩu");
        return;
    }

    try{
        btnLogin.innerText = "Đang xử lí...";
        btnLogin.disabled = true; //vô hiệu hóa nút bấm

        const response = await fetch(`${API_BASE_URL}/api/Auth/login`,{
                method: 'POST',
                headers:{
                    'Content-Type':'application/json'
                },
                body: JSON.stringify({
                    username: username,
                    password: password
                }) 
        });

        if(response.ok){
            const data = await response.json();

            //Lưu JWT vào trình duyệt
            localStorage.setItem('jwtToken', data.token)

            authSection.classList.add('hidden');
            uploadSection.classList.remove('hidden');

            alert("Đăng nhập thành công")
            
        }
        else{
            alert("Sai tài khoản hoặc mật khẩu");
        }
    }catch (error){
        alert("Lỗi kết nối đến server .NET. Hãy chắc chắn .NET đang chạy!\nChi tiết: " + error.message);
    }finally{
        btnLogin.innerText = "Đăng nhập";
        btnLogin.disabled = false;
    }
});

// --- HÀM 2: KIỂM TRA ĐĂNG NHẬP KHI VỪA MỞ WEB ---
window.onload = () =>{
    const token = localStorage.getItem("jwtToken")
    if(token){
        authSection.classList.add('hidden');
        uploadSection.classList.remove('hidden');
    }
}