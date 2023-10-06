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
        '@node_modules/datatables.net-buttons/js/': '@libs/datatables.net-buttons/js/',
        '@node_modules/datatables.net-buttons-bs5/css/': '@libs/datatables.net-buttons-bs5/css/',
        '@node_modules/datatables.net-buttons-bs5/js/': '@libs/datatables.net-buttons-bs5/js/' ,
        '@node_modules/datatables.net-select/js/': '@libs/datatables.net-select/js/',
        '@node_modules/datatables.net-select-bs5/css/': '@libs/datatables.net-select-bs5/css/',
        '@node_modules/datatables.net-select-bs5/js/': '@libs/datatables.net-select-bs5/js/',
        '@node_modules/jspdf/dist/': '@libs/jspdf/dist',
        '@node_modules/html2canvas/dist/': '@libs/html2canvas/dist'
    },
};