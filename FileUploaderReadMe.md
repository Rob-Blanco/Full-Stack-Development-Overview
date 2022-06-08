# File Uploader Explanation 
 
FileUploader.jsx is a functional component which will upload one or multiple files to the Sabio Amazon Web Service 'bucket'
 (the bucket is a container for objects stored in Amazon S3, or Amazon Simple Storage Service). 

## Import File Uploader
To use, first import 'FileUploader.jsx' to the component on which you want it rendered.
Ex: Import FileUploader from '../components/fileuploader/FileUploader.jsx'.

## Create a success handler for the response  
Declare a success handler in your component which will route FileUploader.jsx's response. This response is being returned from the fileService.js service file in the services folder. Below is an example success handler: 

    const onHandleUploadSuccess = (data) => {
        _logger('File Upload Success', data.items);
    };

There are already toastify success and error handlers defined in the File Uploader component. 
The success handler you create will need to be passed as a prop to the actual File Uploader itself, if you want any response information. 

So for example, passed as a prop in the return statement of your own component:

`<FileUploader onHandleUploadSuccess={onHandleUploadSuccess}></FileUploader>`

## Add the File Uploader component to your return statement and pass your success handler as a prop
Similar to as described above:

    return (
        <React.Fragment>
            <FileUploader onHandleUploadSuccess={onHandleUploadSuccess}></FileUploader>
        </React.Fragment>
    );

## Return data
Response data will be an array of 'items' with objects of properties "Id" and "Url". Multiple files can be uploaded, either highlight, click/drag multiple files. 
Or click on the dropzone and CTRL click multiple files. 

## Logger within FileUploader
The _logger within FileUploader.jsx is extended to ('UserFileUploader')

## Toastify Handlers
There are already success and file type error handlers defined in FileUploader.jsx. The most common error would be a File Type Error, which is elaborated on in File Type Considerations, below.

## File Type Considerations
The FileUploader is currently configured, within .NET and SQL, to accept PDF, text files (not Word documents, just simple text files from Notepad - for example), 
images (png, JPEG, JPG), and audio. If you want to upload videos or MS Office documents, you will need to configure additional logic within 'GetFileType()' from within FileService.
Which is currently not the intent of the component - which is, to upload pictures of residences and listings.  

## Full Example 
import React from "react";
import FileUploader from "./components/fileuploader/FileUploader";

const FileUploaderTest = () => {
  
  const onHandleUploadSuccess = (data) => {
    <The array of Objects with properties of Id and Urls can be manipulated here>
  };

  return (
    <React.Fragment>
      <FileUploader
        onHandleUploadSuccess={onHandleUploadSuccess}
      ></FileUploader>
    </React.Fragment>
  );
};

export default FileUploaderTest;
## Testing in Postman
If the need to test in Postman arises, use the following URL:
"http://localhost:{whatever the number for your local host is}}/api/files"

Within the KEY column of Postman, add a single row. With a KEY value of "files"
Within the value column, click on 'Select Files'. Then if you want to upload multiple files, CTRL + Click whatever number of files you want to upload. 

Trying to upload with multiple rows of "Keys" will not pass the information that the FileUploader controller is looking for. So, only pass one row of a KEY value 'files' and insert the number of files you want into that one 'Select Files' dialogue box. 
