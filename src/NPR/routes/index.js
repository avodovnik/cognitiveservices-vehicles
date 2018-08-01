var config = require("../config/config.js");
var express = require('express');
const fileUpload = require('express-fileupload');
var request = require('request');

var router = express.Router();
router.use(fileUpload());

/***************************************************************************
    Home page
***************************************************************************/
router.get('/', function (req, res, next) {
  res.render('index', {
    config: config,
    success: req.query.success
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
      return res.redirect("/?success=false");
    } else {
      var funcResult = JSON.parse(response.body);
      console.log('Response: ', funcResult.PlateContent);

      // TODO: this is where we send the message to the storage queue

      return res.redirect("/?success=true");
    }
  });

});


module.exports = router;
