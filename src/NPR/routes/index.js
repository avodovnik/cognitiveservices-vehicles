var config = require("../config/config.js");
var express = require('express');
const fileUpload = require('express-fileupload');
var request = require('request');

var router = express.Router();
var azure = require('azure-storage');
var queueSvc = azure.createQueueService(process.env.AZURE_STORAGE_ACCOUNT_NAME, process.env.AZURE_STORAGE_ACCOUNT_KEY);

const QueueMessageEncoder = azure.QueueMessageEncoder;
console.log(queueSvc.messageEncoder);
queueSvc.messageEncoder = new QueueMessageEncoder.TextBase64QueueMessageEncoder();


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

			//Send response content 
			var lane = funcResult.Lane; 
			var platecontent = funcResult.PlateContent;
			
			//create queue message
			var message = {
					Lane: lane,
					PlateContent: platecontent,
					Time: new Date(),
			};

			queueSvc.createMessage(process.env.AZURE_STORAGE_QUEUE_NAME, JSON.stringify(message), function (error) {
					console.log(error)
					if (!error) {
							console.log("success")
					}

					return res.redirect("/?success=false");
			});

			return res.redirect("/?success=true");
		}
	});

});


module.exports = router;
