function handleSearch(e, tableName) {
    let filterValue = e.currentTarget.value;
    let oTable = $('#'+tableName).dataTable();
    oTable.fnFilter(filterValue);
}

