function closeFullScreenPopup(){
    document.getElementById('fullScreenPopup').remove();
}

async function displayAddPostWindow(target) {
    const formWindow = fullScreenPopup();
}

async function displayViewPostWindow(target) {
    const postWindow = fullScreenPopup();
    response = await fetchRequest({"type": "loadPostPage", "postId": target});
    postWindow.innerHTML = response["html"];
    document.getElementById("submitComment").addEventListener("click", function() {submitPostComment()});
}

async function fetchRequest(argsObject){
    const url = '/fetch';

    responseData = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'text/plain' },
        body: JSON.stringify(argsObject)
    })

    temp = await responseData.text();
    temp2 = await JSON.parse(temp);
    return temp2;
}

function fullScreenPopup(){
    const fullPopup = document.getElementsByTagName('body')[0].appendChild(document.createElement('div'));
    fullPopup.outerHTML = '<div id="fullScreenPopup"><div id="fullPopupInner"><div id="popupCloseBox">X</div><div id="fullPopupContentWrapper">Content Loading...</div></div></div>';

    document.getElementById('fullScreenPopup').addEventListener('click', function() {closeFullScreenPopup()});
    document.getElementById('fullPopupInner').addEventListener('click', function(Event) {Event.stopPropagation();});
    document.getElementById('popupCloseBox').addEventListener('click', function() {closeFullScreenPopup()});
    return document.getElementById('fullPopupContentWrapper');
}

function getCookie(key){
    return document.cookie
        .split("; ")
        .find((row) => row.startsWith(key + "="))
        ?.split("=")[1];
}

async function submitPostComment(){
    const button = document.getElementById("submitComment");
    const postId = button.attributes.target.value;
    const nonce = button.attributes.nonceVal.value;
    const commentText = document.getElementById("addCommentText").value;

    response = await fetchRequest({"type": "submitPostComment", "postId": postId, "commentText": commentText, "nonceVal": nonce});
    // confirm submission (display error), clear text box, trigger comment reload
}