module.exports = {
    queue: {
        account: process.env.AZURE_STORAGE_ACCOUNT_NAME,
        accessKey: process.env.AZURE_STORAGE_ACCOUNT_KEY,
        queueName: process.env.AZURE_STORAGE_QUEUE_NAME
    }
};