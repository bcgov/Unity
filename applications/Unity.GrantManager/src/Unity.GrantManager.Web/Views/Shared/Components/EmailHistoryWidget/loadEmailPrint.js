function executeOperations() {
    $("input").attr("disabled", true);
    disableLinks();

    setTimeout(function () {
        globalThis.print();
    }, 1000);
}

function disableLinks() {
    let links = document.querySelectorAll('a');
    links.forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
        });
        link.style.pointerEvents = 'none';
        link.style.color = 'gray';
    });
}
