﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Storage.Blob.Cmdlet
{
    using System;
    using System.Management.Automation;
    using System.Security.Permissions;
    using Microsoft.WindowsAzure.Commands.Storage.Common;
    using Microsoft.WindowsAzure.Commands.Storage.Model.Contract;
    using Microsoft.WindowsAzure.Storage.Blob;

    [Cmdlet(VerbsCommon.New, StorageNouns.ContainerSas), OutputType(typeof(String))]
    public class NewAzureStorageContainerSasTokenCommand : StorageCloudBlobCmdletBase
    {
        /// <summary>
        /// Sas permission parameter set name
        /// </summary>
        private const string SasPermissionParameterSet = "SasPermission";

        /// <summary>
        /// Sas policy paremeter set name
        /// </summary>
        private const string SasPolicyParmeterSet = "SasPolicy";

        [Alias("N", "Container")]
        [Parameter(Position = 0, Mandatory = true,
            HelpMessage = "Container Name",
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter(HelpMessage = "Policy Identifier", ParameterSetName = SasPolicyParmeterSet)]
        public string Policy
        {
            get { return accessPolicyIdentifier; }
            set { accessPolicyIdentifier = value; }
        }
        private string accessPolicyIdentifier;

        [Parameter(HelpMessage = "Permissions for a container. Permissions can be any not-empty subset of \"rwdl\".",
            ParameterSetName = SasPermissionParameterSet)]
        public string Permission { get; set; }

        [Parameter(HelpMessage = "Start Time")]
        public DateTime? StartTime { get; set; }

        [Parameter(HelpMessage = "Expiry Time")]
        public DateTime? ExpiryTime { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Display full uri with sas token")]
        public SwitchParameter FullUri { get; set; }

        // Overwrite the useless parameter
        public override int? ServerTimeoutPerRequest { get; set; }
        public override int? ClientTimeoutPerRequest { get; set; }
        public override int? ConcurrentTaskCount { get; set; }


        /// <summary>
        /// Initializes a new instance of the NewAzureStorageContainerSasCommand class.
        /// </summary>
        public NewAzureStorageContainerSasTokenCommand()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the NewAzureStorageContainerSasCommand class.
        /// </summary>
        /// <param name="channel">IStorageBlobManagement channel</param>
        public NewAzureStorageContainerSasTokenCommand(IStorageBlobManagement channel)
        {
            Channel = channel;
            EnableMultiThread = false;
        }

        /// <summary>
        /// Execute command
        /// </summary>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public override void ExecuteCmdlet()
        {
            if (String.IsNullOrEmpty(Name)) return;
            CloudBlobContainer container = Channel.GetContainerReference(Name);
            SharedAccessBlobPolicy accessPolicy = new SharedAccessBlobPolicy();
            bool shouldSetExpiryTime = SasTokenHelper.ValidateContainerAccessPolicy(Channel, container.Name, accessPolicy, accessPolicyIdentifier);
            SetupAccessPolicy(accessPolicy, shouldSetExpiryTime);
            string sasToken = container.GetSharedAccessSignature(accessPolicy, accessPolicyIdentifier);

            if (FullUri)
            {
                string fullUri = SasTokenHelper.GetFullUriWithSASToken(container.Uri.AbsoluteUri.ToString(), sasToken);
                WriteObject(fullUri);
            }
            else
            {
                WriteObject(sasToken);
            }
        }

        /// <summary>
        /// Update the access policy
        /// </summary>
        /// <param name="policy">Access policy object</param>
        /// <param name="shouldSetExpiryTime">Should set the default expiry time</param>
        private void SetupAccessPolicy(SharedAccessBlobPolicy policy, bool shouldSetExpiryTime)
        {
            DateTimeOffset? accessStartTime;
            DateTimeOffset? accessEndTime;
            SasTokenHelper.SetupAccessPolicyLifeTime(StartTime, ExpiryTime,
                out accessStartTime, out accessEndTime, shouldSetExpiryTime);
            policy.SharedAccessStartTime = accessStartTime;
            policy.SharedAccessExpiryTime = accessEndTime;
            SetupAccessPolicyPermission(policy, Permission);
        }

        /// <summary>
        /// Set up access policy permission
        /// </summary>
        /// <param name="policy">SharedAccessBlobPolicy object</param>
        /// <param name="permission">Permission</param>
        internal void SetupAccessPolicyPermission(SharedAccessBlobPolicy policy, string permission)
        {
            if (string.IsNullOrEmpty(permission)) return;
            policy.Permissions = SharedAccessBlobPermissions.None;
            permission = permission.ToLower();
            foreach (char op in permission)
            {
                switch(op)
                {
                    case StorageNouns.Permission.Read:
                        policy.Permissions |= SharedAccessBlobPermissions.Read;
                        break;
                    case StorageNouns.Permission.Write:
                        policy.Permissions |= SharedAccessBlobPermissions.Write;
                        break;
                    case StorageNouns.Permission.Delete:
                        policy.Permissions |= SharedAccessBlobPermissions.Delete;
                        break;
                    case StorageNouns.Permission.List:
                        policy.Permissions |= SharedAccessBlobPermissions.List;
                        break;
                    default:
                        throw new ArgumentException(string.Format(Resources.InvalidAccessPermission, op));
                }
            }
        }
    }
}