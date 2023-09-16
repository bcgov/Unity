module.exports = {
    aliases: {
        '@node_modules': './node_modules',
        '@libs': './wwwroot/libs',
    },
    clean: [],
    mappings: {
        '@node_modules/formiojs/dist/': '@libs/formiojs',
        '@node_modules/axios/dist/': '@libs/axios',
        '@node_modules/pubsub-js/src': '@libs/pubsub-js/src',
        '@node_modules/datatables.net-bs5/images/': '@libs/datatables.net-bs5/images/',
        '@node_modules/datatables.net-buttons/js/dataTables.buttons.js': '@libs/datatables.net-buttons/js/',
        '@node_modules/datatables.net-buttons/js/buttons.colVis.js': '@libs/datatables.net-buttons/js/',
        '@node_modules/datatables.net-buttons/js/buttons.html5.js': '@libs/datatables.net-buttons/js/',
        '@node_modules/datatables.net-buttons-bs5/css/buttons.bootstrap5.css': '@libs/datatables.net-buttons-bs5/css/',
        '@node_modules/datatables.net-buttons-bs5/js/buttons.bootstrap5.js': '@libs/datatables.net-buttons-bs5/js/' ,
        '@node_modules/datatables.net-select/js/dataTables.select.js': '@libs/datatables.net-select/js/',
        '@node_modules/datatables.net-select-bs5/css/select.bootstrap5.css': '@libs/datatables.net-select-bs5/css/',
        '@node_modules/datatables.net-select-bs5/js/select.bootstrap5.js': '@libs/datatables.net-select-bs5/js/'
    },
};