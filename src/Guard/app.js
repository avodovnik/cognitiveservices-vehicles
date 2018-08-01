// configuration stuff from env variables
require('dotenv').config();

var express = require('express');
var path = require('path');
var http = require('http');

var app = express();

var numplateregistered = "";
var nprresultraw = "";
var npverified = false; //true or false (has np been verified)
var faceverified = false; //true or false (has it been verified)


//allow post direct to app with API results
app.post('/faceresult', function(request, response){
    
    //TODO: set faceresult raw = true if face is verified
    
    console.log(request.body);      // your JSON
    response.send(request.body);    // echo the result back
});  

app.post('/nprresult', function(request, response){

    npverified = (nprresultraw.includes(numplateregistered));

    //TODO: set faceresult raw = true if face is verified

    console.log(request.body);      // your JSON
    response.send(request.body);    // echo the result back
});  

function checkAPIresults(){
    if(faceverified == true && npverified == true){
        return true, lane; //lane and boolean response returned 
    } else {
        return false, lane; //"check car in lane " + lane;
    }
}

// setup the view engine
app.set('views', path.join(__dirname, 'views'));
app.set('view engine', 'ejs');

app.use(express.static(path.join(__dirname, 'public')));

app.use('/', require('./routes/index'));

module.exports = app;
