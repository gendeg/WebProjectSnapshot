let view = "desktop";

document.addEventListener('DOMContentLoaded', InitialResizeHandler);

function InitialResizeHandler(){
    resizeHandler();
}

async function resizeHandler(){
    const currentWidth = window.innerWidth;
    if (currentWidth < 1024 && view == "desktop"){
        changeToMobileView();
    }
    if (currentWidth >= 1024 && view == "mobile"){
        changeToDesktopView();
    }
}

async function changeToMobileView(){
    document.getElementById("carousel").style.display = "flex";
    const plazaContent = document.getElementById('plazaContent');
    let first = true;
    for (const child of plazaContent.children){
        if (!first) child.style.display = "none";
        first = false;
    }
    plazaContent.style.display = "block";
    document.documentElement.style.fontSize = "8px";
    document.getElementsByTagName("body")[0].style.fontSize = "2rem";
    view = "mobile";
}

async function changeToDesktopView(){
    document.getElementById("carousel").style.display = "none";
    const plazaContent = document.getElementById('plazaContent');
    plazaContent.style.display = "grid";
    for (const child of plazaContent.children){
        child.style.display = "block";
    }
    document.documentElement.style.fontSize = "16px";
    document.getElementsByTagName("body")[0].style.fontSize = "1rem";
    view = "desktop";
}

async function carouselClick(target){
    for (const child of plazaContent.children){
        child.style.display = "none";
    }
    document.getElementById(target).style.display = "block";
}

async function addCarouselEventListeners(){
    const carousel = document.getElementById('carousel');
    const carouselItems = carousel.querySelector('.carouselItems');

    const carouselCards = carousel.querySelectorAll('[caroTarget]');
    carouselCards.forEach(card => {
        const target = card.attributes.caroTarget.value;
        card.addEventListener('click', function() {carouselClick(target)});
    });
    
    carousel.querySelector('.scrollBtn.left').addEventListener('click', () => {
        carouselItems.scrollBy({ left: -180, behavior: 'smooth' });
    });
    carousel.querySelector('.scrollBtn.right').addEventListener('click', () => {
        carouselItems.scrollBy({ left: 180, behavior: 'smooth' });
    });
}