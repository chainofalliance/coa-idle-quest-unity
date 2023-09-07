const
    connect = require('connect'),
    serveStatic = require('serve-static'),
    unitywebEncoding = require('unityweb-encoding'),
    basicAuth = require('basic-auth-connect');
const pathRoot = ".";
connect()
    .use(basicAuth('coa_demo', 'nitro'))
    .use(unitywebEncoding.serveHeader(pathRoot))
    .use(serveStatic(pathRoot))
    .listen(3000);