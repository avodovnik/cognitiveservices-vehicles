module.exports = {
    face : {
        key: process.env.FACE_API_KEY
    },
    function : {
        url : process.env.FUNCTION_URL || 'https://modnpr.azurewebsites.net/api/NPRFunction'
    },
    queue: {
        account: process.env.AZURE_STORAGE_ACCOUNT_NAME,
        accessKey: process.env.AZURE_STORAGE_ACCOUNT_KEY,
        queueName: process.env.AZURE_STORAGE_QUEUE_NAME
    }
  };