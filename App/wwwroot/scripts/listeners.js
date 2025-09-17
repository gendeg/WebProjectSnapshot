window.addEventListener('resize', resizeHandler);
addCarouselEventListeners();

document.querySelectorAll('[addPostTarget]').forEach( button => {
    const target = button.attributes.addPostTarget.value;
    button.addEventListener('click', function() {displayAddPostWindow(target)});
});

document.querySelectorAll('[viewPostTarget]').forEach( button => {
    const target = button.attributes.viewPostTarget.value;
    button.addEventListener('click', function() {displayViewPostWindow(target)});
});

document.getElementById("submitComment").addEventListener(function() {submitPostComment()})
