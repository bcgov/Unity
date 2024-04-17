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
        '@node_modules/datatables.net-bs5/': '@libs/datatables.net-bs5/',
        '@node_modules/datatables.net-buttons/': '@libs/datatables.net-buttons/',
        '@node_modules/datatables.net-buttons-bs5/': '@libs/datatables.net-buttons-bs5/',
        '@node_modules/datatables.net-select/': '@libs/datatables.net-select/',
        '@node_modules/datatables.net-select-bs5/': '@libs/datatables.net-select-bs5/',
        '@node_modules/datatables.net-fixedheader-bs5/': '@libs/datatables.net-fixedheader-bs5/',
        '@node_modules/datatables.net-colreorder/': '@libs/datatables.net-colreorder/',
        '@node_modules/datatables.net-colreorder-bs5/': '@libs/datatables.net-colreorder-bs5/',

        '@node_modules/jspdf/dist/': '@libs/jspdf/dist',
        '@node_modules/html2canvas/dist/': '@libs/html2canvas/dist',
        '@node_modules/sweetalert2/src/': '@libs/sweetalert2/src',
        '@node_modules/jquery-maskmoney/': '@libs/jquery-maskmoney',
        '@node_modules/datatables.net-fixedheader/js': '@libs/datatables.net-fixedheader/js/',        
        "@node_modules/echarts/dist/echarts.min.js": "@libs/echarts/",
        '@node_modules/bootstrap-4/dist': '@libs/bootstrap-4/dist/'
    },
};