//Test function
function handleSubmit(event) {
    event.preventDefault(); 

    const resultText = document.getElementById("resultText").value;

    alert("Submitted text:\n" + resultText);

    // Just in case you also need to send it to a server endpoint using fetch:
    /*
    fetch('/api/submit', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ result: resultText })
    }).then(response => response.json())
      .then(data => console.log(data));
    */
}
