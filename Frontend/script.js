const API_BASE_URL = "http://localhost:5288";

const txtUsername = document.getElementById('username');
const txtPassword = document.getElementById('password');
const btnLogin = document.getElementById('btnLogin');
const authSection = document.getElementById('authSection');
const uploadSection = document.getElementById('uploadSection');
const fileInput = document.getElementById('fileInput');
const btnUpload = document.getElementById('btnUpload');
const documentList = document.getElementById('documentList');
const btnLogout = document.getElementById('btnLogout');
const txtQuery = document.getElementById('txtQuery');
const btnSend = document.getElementById('btnSend');
const chatHistoryUI = document.getElementById('chatHistory');
const docSelector = document.getElementById('docSelector');

let chatHistoryArray = [];

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

            loadDocuments();

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

        loadDocuments();
    }

}

// --- HÀM 3: XỬ LÝ UPLOAD FILE ---
btnUpload.addEventListener('click', async() =>{
    if(fileInput.files.length === 0){
        alert("Vui lòng chọn file PDF");
        return;
    }

    const file = fileInput.files[0];

    if(file.type !== "application/pdf" && !file.name.toLowerCase().endsWith('.pdf')){
        alert("Chỉ hỗ trợ file PDF");
        return;
    }

    const token = localStorage.getItem('jwtToken')
    if(!token){
        alert("Bạn chưa đăng nhập hoặc phiên bản đăng nhập đã hết hạn");
        return;
    }

    const formData = new FormData();
    formData.append("file",file);

    try{
        btnUpload.innerText = "Đang nạp tài liệu cho AI";
        btnUpload.disabled = true;

        const response = await fetch(`${API_BASE_URL}/api/Documents/upload`,{
            method: 'POST',
            headers:{
                'Authorization': `bearer ${token}`
            },
            body: formData
        });

        if(response.ok){
            const data = await response.json();
            alert("Tải lên thành công");

            fileInput.value="";

            const li = document.createElement('li');
            li.innerHTML = `<i class="fa-solid fa-file-pdf"></i> ${data.fileName}`;
            documentList.appendChild(li)
        }else{
            const errorText = await response.text();
            alert("Lỗi: " +errorText);
        }
    }catch(error){
        alert("Lỗi kết nối. Chi tiết: " + error.message);
    }finally {
        // Trả nút về trạng thái ban đầu
        btnUpload.innerHTML = `<i class="fa-solid fa-upload"></i> Tải lên PDF`;
        btnUpload.disabled = false;
    }
})

// --- HÀM 4: TẢI DANH SÁCH TÀI LIỆU TỪ SERVER ---
async function loadDocuments() {
    const token = localStorage.getItem("jwtToken");
    
    if(!token){
        alert("Lỗi Token đăng nhập")
        return;
    }

    try{
        const response = await fetch (`${API_BASE_URL}/api/Documents`,{
            method: 'GET',
            headers: {
                'Authorization': `bearer ${token}`
            }
        });

        if(response.ok){
            const documents = await response.json();

            documentList.innerHTML = '';

            docSelector.innerHTML = '<option value="">-- Chọn tài liệu --</option>';

            documents.forEach((doc,index) =>{
                const stt = index + 1;
                const li = document.createElement('li');
                li.innerHTML = `<span class="stt">${stt}.</span><i class ="fa-solid fa-file-pdf"></i>${doc.fileName}`;
                documentList.appendChild(li);

                //Đổ dữ liệu vào ô select
                const option = document.createElement('option');
                option.value = doc.id;
                option.textContent = doc.fileName;
                docSelector.appendChild(option);

            });
        }
    }catch (error) {
        console.error("Lỗi khi tải danh sách file:", error);
    }
}

// --- HÀM 5: XỬ LÝ ĐĂNG XUẤT ---
btnLogout.addEventListener('click', () => {
    // 1. Xóa vé trong ví
    localStorage.removeItem('jwtToken');
    
    // 2. Xóa sạch danh sách file trên màn hình để bảo mật
    documentList.innerHTML = '';
    
    // 3. Đổi lại giao diện
    uploadSection.classList.add('hidden');
    authSection.classList.remove('hidden');
    
    // 4. Xóa trắng form đăng nhập
    txtUsername.value = '';
    txtPassword.value = '';
});

// --- HÀM 6: VẼ TIN NHẮN LÊN MÀN HÌNH ---
function appendMessageToUI(role, text){
    const messageDiv = document.createElement('div');
    messageDiv.classList.add('message');

    if(role==='user'){
        messageDiv.classList.add('user-message');
        messageDiv.innerHTML =`
            <div class="avatar"><i class="fa-solid fa-user"></i></div>
            <div class="content">${text}</div>
        `;
    }else{
        messageDiv.classList.add('ai-message');
        messageDiv.innerHTML=`
            <div class="avatar"><i class="fa-solid fa-robot"></i></div>
            <div class="content">${text}</div>
        `;
    }

    chatHistoryUI.appendChild(messageDiv);
    chatHistoryUI.scrollTop = chatHistoryUI.scrollHeight;
    return messageDiv;
}

//Xử lí sự kiện nút bấm gửi
btnSend.addEventListener('click', handleSendChat);

txtQuery.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        handleSendChat();
    }
});

// --- HÀM 7: GỬI CÂU HỎI CHO AI ---
async function handleSendChat() {
    const query = txtQuery.value.trim();
    if(!query){
        alert("Vui lòng nhập câu hỏi");
        return;
    }

    appendMessageToUI('user', query);
    txtQuery.value ='';

    const documentId = docSelector.value || null;
    const token = localStorage.getItem('jwtToken');

    if (!token) {
        appendMessageToUI('ai', "Lỗi: Bạn chưa đăng nhập!");
        return;
    }
    
    btnSend.disabled = true;
    const typingMessage = appendMessageToUI('ai', '<i class="fa-solid fa-ellipsis fa-fade"></i> AI đang suy nghĩ...');

    try{
        const response = await fetch(`${API_BASE_URL}/api/Chat/ask`,{
            method : 'POST',
            headers :{
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                query:query,
                document_id:documentId,
                history:chatHistoryArray
            })
        });

        if (typingMessage) {
            typingMessage.remove();
        }

        if(response.ok){
            const data = await response.json();

            appendMessageToUI('ai',data.answer)

            chatHistoryArray.push({ role: 'user', content: query });
            chatHistoryArray.push({ role: 'ai', content: data.answer });     
        }else{
            const errorText = await response.text();
            appendMessageToUI('ai', `Đã xảy ra lỗi: ${errorText}`);
        }
    }catch (error) {
        typingMessage.remove();
        appendMessageToUI('ai', `Lỗi mạng: ${error.message}`);
    }finally {
        btnSend.disabled = false;
        txtQuery.focus();
    }

}