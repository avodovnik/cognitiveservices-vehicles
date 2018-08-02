var express = require('express');
var router = express.Router();
var config = require("../config/config.js");
var azure = require('azure-storage');

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

router.post('/send', function (req, res, next) {
    console.log("Send received.");
    // var body = JSON.parse(req.body);

    console.log(req.body);

    //Send response content 
    var lane = config.lane;

    //create queue message
    var message = {
        Lane: lane,
        IsVerified: req.body.result,
        Confidence: req.body.confidence,
        Time: new Date(),
    };

    var queueSvc = azure.createQueueService(config.queue.account, config.queue.accessKey);

    const QueueMessageEncoder = azure.QueueMessageEncoder;
    queueSvc.messageEncoder = new QueueMessageEncoder.TextBase64QueueMessageEncoder();

    queueSvc.createMessage(config.queue.queueName, JSON.stringify(message), function (error) {
        console.log(error)
        if (!error) {
            console.log("success");
            res.send('{ result: "done" }');
            // res.end();
        } else {
            return res.redirect("/?success=false");
        }
    });
});

module.exports = router;
