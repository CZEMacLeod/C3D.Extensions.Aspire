require('./intrumentation'); // Ensure this is imported before other modules

var express = require('express');
var app = express();
//var otlp = require('./intrumentation');

app.get('/', function (req, res) {
    res.send('Hello World!');
});
app.listen(3030, function () {
    console.log('Example app listening on port 3030!');
});