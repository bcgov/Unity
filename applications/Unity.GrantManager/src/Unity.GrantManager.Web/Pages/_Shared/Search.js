function handleSearch(e, tableName) {
    console.log('handleSearch:' + tableName);
    let filterValue = e.currentTarget.value;
    let oTable = $('#'+tableName).dataTable();
    oTable.fnFilter(filterValue);
}

