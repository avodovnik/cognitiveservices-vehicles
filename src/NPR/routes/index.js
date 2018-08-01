var config = require("../config/config.js");
var express = require('express');
const fileUpload = require('express-fileupload');
var request = require('request');

var router = express.Router();
router.use(fileUpload());

//TODO: send NPRFunction result to storage queue

/***************************************************************************
    Home page
***************************************************************************/
router.get('/', function (req, res, next) {

  var app;

  app = req.app;
  res.render('index', {
    config: config
  });
});

//handle form input (e.g uploads)
router.post("/upload", function (req, res) {
  console.log("Upload called.");
  if (!req.files) {
    return res.status(400).send('No files were uploaded.');
  }


  request({
    url: config.function.url + "?lane=" + req.body.laneNumber,
    method: 'POST',
    body: req.files.plate.data,
    encoding: null
  }, (error, response, body) => {
    if (error) {
      console.log('Error sending message: ', error)
    } else {
      var funcResult = JSON.parse(response.body);
      console.log('Response: ', funcResult.PlateContent);
    }
  });

});


module.exports = router;
