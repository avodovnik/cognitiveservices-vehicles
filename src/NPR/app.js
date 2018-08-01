// configuration stuff from env variables
require('dotenv').config();

var express = require('express');
var path = require('path');
const multer = require("multer");
const fs = require("fs");
const http = require("http");

var app = express();

// setup the view engine
app.set('views', path.join(__dirname, 'views'));
app.set('view engine', 'ejs');

app.use(express.static(path.join(__dirname, 'public')));

app.use('/', require('./routes/index'));

app.get("/", express.static(path.join(__dirname, "./views")));

const httpServer = http.createServer(app);

const PORT = process.env.PORT || 4000;

httpServer.listen(4000, () => {
  console.log(`Server is listening on port ${PORT}`);
});

const handleError = (err, res) => {
    res
      .status(500)
      .contentType("text/plain")
      .end("Oops! Something went wrong!");
};
  
const upload = multer({
    dest: "./uploads/"
    // you might also want to set some limits: https://github.com/expressjs/multer#limits
});
  
//TODO: send NPRFunction result to storage queue

//TODO: send upload to NPRFunction
//Takes lane as param
function npOCR(lane){

    var data = fs.readFileSync('./uploads/image.jpg');

    $.ajax({
      type: 'POST',
      data: JSON.stringify(data),
          contentType: 'application/octet-stream',
                  url: 'https://modnpr.azurewebsites.net/api/NPRFunction?lane='+lane,						
                  success: function(result) {
                      console.log('success');
                      console.log(JSON.stringify(result));
                  }
    });

}

//handle form input (e.g uploads)
app.post(
  "/upload",
  upload.single("file" /* name attribute of <file> element in your form */),
  (req, res) => {
    const tempPath = req.file.path;
    const targetPath = path.join(__dirname, "./uploads/image.jpg");
  
    if (path.extname(req.file.originalname).toLowerCase() === ".jpg" || ".jpeg") {
      fs.rename(tempPath, targetPath, err => {
        if (err) return handleError(err, res);
  
        res
          .status(200)
            .contentType("text/plain")
            .end("File uploaded!");
        });
      } else {
        fs.unlink(tempPath, err => {
        if (err) return handleError(err, res);
  
        res
          .status(403)
          .contentType("text/plain")
          .end("Only .jpg or .jpeg files are allowed!");
      });
    }
  },
); 

module.exports = app;
