// configuration stuff from env variables
require('dotenv').config();

var express = require('express');
var path = require('path');
const fileUpload = require('express-fileupload');
 
var app = express();

// setup the view engine
app.set('views', path.join(__dirname, 'views'));
app.set('view engine', 'ejs');

app.use(express.static(path.join(__dirname, 'public')));

app.use('/', require('./routes/index'));

// setup file uploads
app.use(fileUpload());

module.exports = app;