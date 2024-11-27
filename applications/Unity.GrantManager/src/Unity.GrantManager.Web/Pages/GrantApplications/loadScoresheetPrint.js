
function executeOperations(data) {
  
    $('button').hide();
    $("input").attr("disabled", true);
    $("select").attr("disabled", true);
    $("textarea").attr("disabled", true);
    disableLinks();

    setTimeout(function () {
        window.print();
    }, 1000);
}

function disableLinks() {
    let links = document.querySelectorAll('a');
    links.forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
        });
        link.style.pointerEvents = 'none'; // Optionally style the link to indicate it's disabled
        link.style.color = 'gray'; // Optionally change the color to indicate it's disabled
    });
}


