using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sabio.Data;
using Sabio.Data.Providers;
using Sabio.Models;
using Sabio.Models.Domain.Configs;
using Sabio.Models.Domain.Files;
using Sabio.Models.Requests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Sabio.Services
{
    public class FileService : IFileService
    {
        private IDataProvider _data = null;
        private IOptions<AWSStorageConfig> _storageConfig = null; 
                
        public FileService(IDataProvider data, IOptions<AWSStorageConfig> storageConfig)
        {
           _data = data;
           _storageConfig = storageConfig;
        }

        public List<FileBase> Upload (List<IFormFile> files, int userId)
        {
            List<FileBase> responsePackage = new List<FileBase>();

            string AWSDomain = _storageConfig.Value.Domain;
            string AWSAccessKey = _storageConfig.Value.AccessKey;
            string AWSSecret = _storageConfig.Value.Secret;
            string AWSBucket = _storageConfig.Value.BucketName;
            string region = _storageConfig.Value.BucketRegion;
            string projectTitle = _storageConfig.Value.ProjectTitle;

            RegionEndpoint AWSBucketRegion = RegionEndpoint.GetBySystemName(region);
            AWSCredentials credentials = new BasicAWSCredentials(AWSAccessKey, AWSSecret);
            AmazonS3Client client = new AmazonS3Client(credentials, AWSBucketRegion);
            TransferUtility transferUtility = new TransferUtility(client);
                        
            responsePackage = UploadFiles(files, userId, responsePackage, AWSDomain, AWSBucket, projectTitle, transferUtility);

            return responsePackage;

        }

        public Paged<File> Get(int pageIndex, int pageSize, int createdBy)
        {
            string procName = "[dbo].[Files_Select_ByCreatedBy]";

            List<File> files = null;
            Paged<File> pagedList = null;
            int totalCount = 0;

            _data.ExecuteCmd(procName,
                delegate (SqlParameterCollection inputParams)
                {
                    inputParams.AddWithValue("@PageIndex", pageIndex);
                    inputParams.AddWithValue("@PageSize", pageSize);
                    inputParams.AddWithValue("@CreatedBy", createdBy);

                },
                singleRecordMapper: delegate (IDataReader reader, short set) 
                {
                    int startingIndex = 0;
                    File file = MapSingleFile(reader, ref startingIndex);

                    if (totalCount == 0)
                    {
                        totalCount = reader.GetSafeInt32(startingIndex++);
                    }

                    if (files == null)
                    {
                        files = new List<File>();
                    }
                    files.Add(file);
                }

            );

            if (files != null)
            {
                pagedList = new Paged<File>(files, pageIndex, pageSize, totalCount);
            }

            return pagedList;
        }

        public Paged<File> Get(int pageIndex, int pageSize)
        {
            string procName = "[dbo].[Files_SelectAll]";

            List<File> files = null;
            Paged<File> pagedList = null;
            int totalCount = 0;

            _data.ExecuteCmd(procName,
                delegate (SqlParameterCollection inputParams)
                {
                    inputParams.AddWithValue("@PageIndex", pageIndex);
                    inputParams.AddWithValue("@PageSize", pageSize);

                },
                singleRecordMapper: delegate (IDataReader reader, short set) 
                {
                    int startingIndex = 0;
                    File file= MapSingleFile(reader , ref startingIndex);
                    if (totalCount == 0)
                    {
                        totalCount = reader.GetSafeInt32(startingIndex++);
                    }

                    if (files == null)
                    {
                        files= new List<File>();
                    }
                    files.Add(file);
                }

            );

            if (files != null)
            {
                pagedList = new Paged<File>(files, pageIndex, pageSize, totalCount);
            }

            return pagedList;
        }

        public void Delete(int id)
        {
            string procName = "[dbo].[Files_Delete_ById]";

            _data.ExecuteNonQuery(procName, inputParamMapper: delegate (SqlParameterCollection col)
            {
                col.AddWithValue("@Id", id);
            }
            );

        }

        public File Get(int id)
        {
            string storedProc = "[dbo].[Files_Select_ById]";
            File file = null;

            _data.ExecuteCmd(storedProc, delegate (SqlParameterCollection inputParamMapper)
            {


                inputParamMapper.AddWithValue("@Id", id);

            }, delegate (IDataReader reader, short set) 
            {
                int startingIndex = 0;
                file = MapSingleFile(reader, ref startingIndex);

            }

            );

            return file;
        }

        public void Update(FileUpdateRequest model, int currentUserId, int id)
        {
            string procName = "[dbo].[Files_Update]";

            _data.ExecuteNonQuery(procName, inputParamMapper: delegate (SqlParameterCollection col)
            {


                AddCommonParams(model, col, currentUserId);
                col.AddWithValue("@Id", id);

            }, returnParameters: null);
        }
        
        private List<FileBase> UploadFiles(List<IFormFile> files, int userId, List<FileBase> responsePackage, string AWSDomain, string AWSBucket, string projectTitle, TransferUtility transferUtility)
        {
            foreach (IFormFile file in files)
            {
                string fileType = GetFileType(file.ContentType);
                string fileKey = Guid.NewGuid().ToString();
                string fileName = $"{projectTitle}/{fileKey}/{file.FileName}";

                FileBase recentAddedFile = UploadFile(userId, AWSDomain, AWSBucket, transferUtility, file, fileType, fileName);
                responsePackage.Add(recentAddedFile);
            }

            return responsePackage;
        }

        private FileBase UploadFile(int userId, string AWSDomain, string AWSBucket, TransferUtility transferUtility, IFormFile file, string fileType, string fileName)
        {
            FileAddRequest model = new FileAddRequest();
            FileBase recentAddedFile = new FileBase();

            transferUtility.Upload(file.OpenReadStream(), AWSBucket, fileName); 

            model.Url = $"{AWSDomain}{fileName}"; ;
            model.FileType = fileType;
            model.CreatedBy = userId;
            
            recentAddedFile.Id = AddToDB(model, userId);
            recentAddedFile.Url = model.Url;
            return recentAddedFile;
        }

        private int AddToDB(FileAddRequest model, int currentUserId)
        {
            int id = 0;
            string storedProc = "[dbo].[Files_Insert]";

            _data.ExecuteNonQuery(
                storedProc,
                inputParamMapper: delegate (SqlParameterCollection col)
                {
                    AddCommonParams(model, col, currentUserId);

                    SqlParameter idOut = new SqlParameter("@Id", SqlDbType.Int);
                    idOut.Direction = ParameterDirection.Output;

                    col.Add(idOut);

                },
                returnParameters: delegate (SqlParameterCollection returnCollection)
                {
                    object oId = returnCollection["@Id"].Value;
                    int.TryParse(oId.ToString(), out id);

                }); ;

            return id;
        }

        private static string GetFileType(string formFileType)
        {
            string fileType = null;
            switch(formFileType)
            {
                case "image/png":
                    fileType = "image";
                    break;
                case "image/jpg":
                    fileType = "image";
                    break;
                case "image/jpeg":
                    fileType = "image";
                    break;
                case "image/webp":
                    fileType = "image";
                    break;
                case "image/bmp":
                    fileType = "image";
                    break;
                case "image/gif":
                    fileType = "image";
                    break;
                case "application/pdf":
                    fileType = "pdf";
                    break;
                case "text/plain":
                    fileType = "text";
                    break;
                case "audio/mpeg":
                    fileType = "audio";
                    break;
                default:
                    throw new Exception("File Type Not Supported");
            }
            return fileType;
        }

        private static void AddCommonParams(FileAddRequest model, SqlParameterCollection col, int currentUserId)
        {
            col.AddWithValue("@Url", model.Url);
            col.AddWithValue("@FileType", model.FileType);
            col.AddWithValue("@CreatedBy", currentUserId);
        }

        private static File MapSingleFile(IDataReader reader, ref int startingIndex)
        {
            File file = new File();
            file.Id = reader.GetSafeInt32(startingIndex++);
            file.Url = reader.GetSafeString(startingIndex++);
            file.FileType = reader.GetSafeString(startingIndex++);
            file.CreatedBy = reader.GetSafeInt32(startingIndex++);
            file.DateCreated = reader.GetSafeDateTime(startingIndex++);

            return file;
        }

    }
}
