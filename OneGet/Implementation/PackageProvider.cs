// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.OneGet.Implementation {
    using System;
    using Api;
    using Packaging;
    using Providers;
    using Utility.Async;
    using Utility.Plugin;

    public class PackageProvider : ProviderBase<IPackageProvider> {
        private string _name;

        internal PackageProvider(IPackageProvider provider) : base(provider) {
        }

        public string Name {
            get {
                return ProviderName;
            }
        }

        public override string ProviderName {
            get {
                return _name ?? (_name = Provider.GetPackageProviderName());
            }
        }

        // Friendly APIs

        public IAsyncEnumerable<PackageSource> AddPackageSource(string name, string location, bool trusted, IHostApi requestObject) {
            return new PackageSourceRequestObject(this, requestObject ?? new object().As<IHostApi>(), request => Provider.AddPackageSource(name, location, trusted, request));
        }

        public IAsyncEnumerable<PackageSource> RemovePackageSource(string name, IHostApi requestObject) {
            return new PackageSourceRequestObject(this, requestObject ?? new object().As<IHostApi>(), request => Provider.RemovePackageSource(name, request));
        }

        public IAsyncEnumerable<SoftwareIdentity> FindPackageByUri(Uri uri, int id, IHostApi requestObject) {
            if (!IsSupportedScheme(uri)) {
                return new EmptyAsyncEnumerable<SoftwareIdentity>();
            }

            return new SoftwareIdentityRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.FindPackageByUri(uri, id, request), Constants.PackageStatus.Available);
        }

        public IAsyncEnumerable<SoftwareIdentity> GetPackageDependencies(SoftwareIdentity package, IHostApi requestObject) {
            return new SoftwareIdentityRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.GetPackageDependencies(package.FastPackageReference, request), Constants.PackageStatus.Dependency);
        }

        public IAsyncEnumerable<SoftwareIdentity> FindPackageByFile(string filename, int id, IHostApi requestObject) {
            if (!IsSupportedFile(filename)) {
                return new EmptyAsyncEnumerable<SoftwareIdentity>();
            }

            return new SoftwareIdentityRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.FindPackageByFile(filename, id, request), Constants.PackageStatus.Available);
        }

        public IAsyncValue<int> StartFind(IHostApi requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            return new FuncRequestObject<int>(this,  requestObject, request => Provider.StartFind(request));
        }

        public IAsyncEnumerable<SoftwareIdentity> CompleteFind(int i, IHostApi requestObject) {
            return new SoftwareIdentityRequestObject(this, requestObject ?? new object().As<IHostApi>(), request => Provider.CompleteFind(i, request), Constants.PackageStatus.Available);
        }

        public IAsyncEnumerable<SoftwareIdentity> FindPackages(string[] names, string requiredVersion, string minimumVersion, string maximumVersion, IHostApi requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            if (names == null) {
                throw new ArgumentNullException("names");
            }

            if (names.Length == 0) {
                return FindPackage(null, requiredVersion, minimumVersion, maximumVersion, 0, requestObject);
            }

            if (names.Length == 1) {
                return FindPackage(names[0], requiredVersion, minimumVersion, maximumVersion, 0, requestObject);
            }

            return new SoftwareIdentityRequestObject(this,  requestObject, request => {
                var id = StartFind(request);
                foreach (var name in names) {
                    Provider.FindPackage(name, requiredVersion, minimumVersion, maximumVersion, id.Value, request);
                }
                Provider.CompleteFind(id.Value, request);
            }, Constants.PackageStatus.Available);
        }

        public IAsyncEnumerable<SoftwareIdentity> FindPackagesByUris(Uri[] uris, IHostApi requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            if (uris == null) {
                throw new ArgumentNullException("uris");
            }

            if (uris.Length == 0) {
                return new EmptyAsyncEnumerable<SoftwareIdentity>();
            }

            if (uris.Length == 1) {
                return FindPackageByUri(uris[0], 0, requestObject);
            }

            return new SoftwareIdentityRequestObject(this,  requestObject , request => {
                var id = StartFind(request);
                foreach (var uri in uris) {
                    Provider.FindPackageByUri(uri, id.Value, request);
                }
                Provider.CompleteFind(id.Value, request);
            }, Constants.PackageStatus.Available);
        }
   
        public IAsyncEnumerable<SoftwareIdentity> FindPackagesByFiles(string[] filenames, IHostApi requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            if (filenames == null) {
                throw new ArgumentNullException("filenames");
            }

            if (filenames.Length == 0) {
                return new EmptyAsyncEnumerable<SoftwareIdentity>();
            }

            if (filenames.Length == 1) {
                return FindPackageByFile(filenames[0], 0, requestObject);
            }

            return new SoftwareIdentityRequestObject(this,  requestObject , request => {
                var id = StartFind(request);
                foreach (var file in filenames) {
                    Provider.FindPackageByFile(file, id.Value, request);
                }
                Provider.CompleteFind(id.Value, request);
            }, Constants.PackageStatus.Available);
        }

        public IAsyncEnumerable<SoftwareIdentity> FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, int id, IHostApi requestObject) {
            return new SoftwareIdentityRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.FindPackage(name, requiredVersion, minimumVersion, maximumVersion, id, request), Constants.PackageStatus.Available);
        }

        public IAsyncEnumerable<SoftwareIdentity> GetInstalledPackages(string name, IHostApi requestObject) {
            return new SoftwareIdentityRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.GetInstalledPackages(name, request), Constants.PackageStatus.Installed);
        }

        public IAsyncEnumerable<SoftwareIdentity> InstallPackage(SoftwareIdentity softwareIdentity, IHostApi requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            if (softwareIdentity == null) {
                throw new ArgumentNullException("softwareIdentity");
            }
            
            // if the provider didn't say this was trusted, we should ask the user if it's ok.
            if (!softwareIdentity.FromTrustedSource) {
                try {
                    if (!requestObject.ShouldContinueWithUntrustedPackageSource(softwareIdentity.Name, softwareIdentity.Source)) {
                        requestObject.Warning(requestObject.FormatMessageString(Constants.Messages.UserDeclinedUntrustedPackageInstall, softwareIdentity.Name));
                        return new EmptyAsyncEnumerable<SoftwareIdentity>();
                    }
                } catch {
                    return new EmptyAsyncEnumerable<SoftwareIdentity>();
                }
            }

            return new SoftwareIdentityRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.InstallPackage(softwareIdentity.FastPackageReference, request), Constants.PackageStatus.Installed);
        }

        public IAsyncEnumerable<SoftwareIdentity> UninstallPackage(SoftwareIdentity softwareIdentity, IHostApi requestObject) {
            return new SoftwareIdentityRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.UninstallPackage(softwareIdentity.FastPackageReference, request), Constants.PackageStatus.Uninstalled);
        }

        public IAsyncEnumerable<PackageSource> ResolvePackageSources(IHostApi requestObject) {
            return new PackageSourceRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.ResolvePackageSources(request));
        }

        public IAsyncAction DownloadPackage(SoftwareIdentity softwareIdentity, string destinationFilename, IHostApi requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            if (softwareIdentity == null) {
                throw new ArgumentNullException("softwareIdentity");
            }

            return new ActionRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.DownloadPackage(softwareIdentity.FastPackageReference, destinationFilename, request));
        }

        internal void ExecuteElevatedAction(string payload, IHostApi requestObject) {
            new ActionRequestObject(this,  requestObject ?? new object().As<IHostApi>(), request => Provider.ExecuteElevatedAction(payload, request)).Wait();
        }
    }
}