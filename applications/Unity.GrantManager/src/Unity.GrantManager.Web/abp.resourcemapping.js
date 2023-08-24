module.exports = {
    aliases: {
        '@node_modules': './node_modules',
        '@libs': './wwwroot/libs',
    },
    clean: [],
    mappings: {
        '@node_modules/formiojs/dist/': '@libs/formiojs',
        '@node_modules/axios/dist/': '@libs/axios',
        '@node_modules/datatables.net-select/js': '@libs/datatables/js',
        '@node_modules/datatables.net-select-bs/js': '@libs/datatables/js',
        '@node_modules/datatables.net-select-bs/css': '@libs/datatables/css',
        '@node_modules/pubsub-js/src': '@libs/pubsub-js/src',
    },
};
