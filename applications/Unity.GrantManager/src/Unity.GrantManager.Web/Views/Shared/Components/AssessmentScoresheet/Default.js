

function parseScoreValueInput(name) {    
    let control = "#" + name;
    return $(control).val() == '' ? 0 : Math.min($(control).attr('max'), $(control).val())
}

function positiveIntegersOnly(e) {    
    if (e.keyCode === 9
        || e.keyCode === 8
        || e.keyCode === 37
        || e.keyCode === 39) {
        return true;
    }
    if(e.target?.value?.length >= 2 ) {
        return false;
    }
    if (!((e.keyCode > 95 && e.keyCode < 106)
        || (e.keyCode > 47 && e.keyCode < 58)
        || e.keyCode == 8)) {
        return false;
    }
}
