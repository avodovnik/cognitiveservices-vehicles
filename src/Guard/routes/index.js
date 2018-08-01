var express = require('express');
var router = express.Router();
var config = require("../config/config.js");
var storage = require('azure-storage');

/***************************************************************************
    Home page
***************************************************************************/
router.get('/', function(req, res, next) {

    var app;

    app = req.app;
    res.render('index', {
        config: config
    });
});

router.get('/poll', function(req, res, next) {
    // poll the queue, see if we have a message,
    var retryOperations = new azure.ExponentialRetryPolicyFilter();
    var queueSvc = azure.createQueueService(config.storage.account, config.storage.accessKey)
                        .withFilter(retryOperations);

    // if we do
    // return the message as JSON
});

module.exports = router;
