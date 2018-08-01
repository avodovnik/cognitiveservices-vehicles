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
var currentlane;

/*allow post direct to app with API results
app.post('/faceresult', function(request, response){
    
    //TODO: set faceresult raw = true if face is verified
    
    console.log(request.body);      //log to console for manual check
    response.send(request.body);    // echo the result back
});  


/*NPR result schema
{
    "Lane": 3,
    "PlateContent": "V12LAF",
    "RequestGuid": "8401eb6a-ead7-4887-82b9-6a45b8d36ed2"
}

app.post('/nprresult', function(request, response){

    console.log(request);
    //console.log(request.body); //log to console for manual check

    
    var request = JSON.parse(request);
    //var body = JSON.parse(request.body);
    console.log(request);
    
    currentlane = body.Lane;
    nprresultraw = body.PlateContent;
    //ignore request GUID for now

    console.log(body);
    console.log(currentlane);
    console.log(nprresultraw);
    

    npverified = (nprresultraw.includes(numplateregistered));
    //verify number plate
    response.send({ status: 'SUCCESS' });
});  

*/

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
