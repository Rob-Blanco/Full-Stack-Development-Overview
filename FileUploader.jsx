import React from 'react';
import Dropzone from 'react-dropzone';
import debug from 'sabio-debug';
import * as fileService from '../../services/fileService';
import PropTypes from 'prop-types';
import { ToastContainer, toast } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

const FileUploader = ({ onHandleUploadSuccess }) => {
    const _logger = debug.extend('UserFileUploader');

    const handleAcceptedFiles = (files) => {
        let formData = new FormData();
        for (let i = 0; i < files.length; i++) {
            _logger('Now adding files to form data', files);
            formData.append('files', files[i]);
        }
        fileService.uploadFile(formData).then(onUploadFileSuccess).catch(onUploadFileError);
    };

    const onUploadFileSuccess = (data) => {
        toast.success('File Upload Success');
        _logger('File Upload Success', data.items);
        onHandleUploadSuccess(data);
    };

    const onUploadFileError = (data) => {
        toast.error('File Upload Error: Please check the file type');
        _logger('File Upload Error', data.response.data.errors);
    };

    return (
        <React.Fragment>
            <ToastContainer />
            <Dropzone onDrop={(acceptedFiles) => handleAcceptedFiles(acceptedFiles)}>
                {({ getRootProps, getInputProps }) => (
                    <div className="dropzone">
                        <div className="dz-message needsclick" {...getRootProps()}>
                            <input {...getInputProps()} />
                            <h5>Drop files here or click to upload.</h5>
                            <span className="text-muted font-13">
                                Please only select Images, such as JPEG, or a PDF.
                            </span>
                        </div>
                    </div>
                )}
            </Dropzone>
        </React.Fragment>
    );
};

FileUploader.propTypes = {
    onHandleUploadSuccess: PropTypes.func,
};

export default FileUploader;
