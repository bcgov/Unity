module.exports = {
    aliases: {
        "@node_modules": "./node_modules",
        "@libs": "./wwwroot/libs"
    },
    clean: [

    ],
    mappings: {
        "@node_modules/formiojs/dist/": "@libs/formiojs",
        "@node_modules/axios/dist/": "@libs/axios"
    }
};
