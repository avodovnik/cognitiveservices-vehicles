module.exports = {
    face : {
        key: process.env.FACE_API_KEY
    },
    queue: {
        account: process.env.AZURE_STORAGE_ACCOUNT_NAME,
        accessKey: process.env.AZURE_STORAGE_ACCOUNT_KEY,
        queueName: process.env.AZURE_STORAGE_QUEUE_NAME
    },
    lane: process.env.LANE
  };