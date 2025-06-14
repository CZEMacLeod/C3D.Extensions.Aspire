﻿using System.Xml.Serialization;

#nullable disable

// The classes in this file are autogenerated from the default template in "%ProgramFiles%\IIS Express\config\templates\PersonalWebServer\applicationhost.config"
// The website https://xmltocsharp.azurewebsites.net/ was used to generate the classes

namespace C3D.Extensions.Aspire.IISExpress.Configuration;

[XmlRoot(ElementName = "section")]
public class Section
{
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }
    [XmlAttribute(AttributeName = "allowDefinition")]
    public string AllowDefinition { get; set; }
    [XmlAttribute(AttributeName = "overrideModeDefault")]
    public string OverrideModeDefault { get; set; }
}

[XmlRoot(ElementName = "sectionGroup")]
public class SectionGroup
{
    [XmlElement(ElementName = "section")]
    public List<Section> Section { get; set; }
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }
    [XmlElement(ElementName = "sectionGroup")]
    public List<SectionGroup> SectionGroups { get; set; }
}

[XmlRoot(ElementName = "configSections")]
public class ConfigSections
{
    [XmlElement(ElementName = "sectionGroup")]
    public List<SectionGroup> SectionGroup { get; set; }
}

[XmlRoot(ElementName = "add")]
public class Add
{
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }
    [XmlAttribute(AttributeName = "type")]
    public string Type { get; set; }
    [XmlAttribute(AttributeName = "description")]
    public string Description { get; set; }
    [XmlAttribute(AttributeName = "keyContainerName")]
    public string KeyContainerName { get; set; }
    [XmlAttribute(AttributeName = "cspProviderName")]
    public string CspProviderName { get; set; }
    [XmlAttribute(AttributeName = "useMachineContainer")]
    public string UseMachineContainer { get; set; }
    [XmlAttribute(AttributeName = "useOAEP")]
    public string UseOAEP { get; set; }
    [XmlAttribute(AttributeName = "sessionKey")]
    public string SessionKey { get; set; }
    [XmlAttribute(AttributeName = "managedRuntimeVersion")]
    public string ManagedRuntimeVersion { get; set; }
    [XmlAttribute(AttributeName = "managedPipelineMode")]
    public string ManagedPipelineMode { get; set; }
    [XmlAttribute(AttributeName = "CLRConfigFile")]
    public string CLRConfigFile { get; set; }
    [XmlAttribute(AttributeName = "autoStart")]
    public string AutoStart { get; set; }
    [XmlAttribute(AttributeName = "value")]
    public string Value { get; set; }
    [XmlAttribute(AttributeName = "image")]
    public string Image { get; set; }
    [XmlAttribute(AttributeName = "preCondition")]
    public string PreCondition { get; set; }
    [XmlAttribute(AttributeName = "mimeType")]
    public string MimeType { get; set; }
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
    [XmlAttribute(AttributeName = "accessType")]
    public string AccessType { get; set; }
    [XmlAttribute(AttributeName = "users")]
    public string Users { get; set; }
    [XmlAttribute(AttributeName = "path")]
    public string Path { get; set; }
    [XmlAttribute(AttributeName = "allowed")]
    public string Allowed { get; set; }
    [XmlAttribute(AttributeName = "groupId")]
    public string GroupId { get; set; }
    [XmlAttribute(AttributeName = "fileExtension")]
    public string FileExtension { get; set; }
    [XmlAttribute(AttributeName = "segment")]
    public string Segment { get; set; }
    [XmlElement(ElementName = "traceAreas")]
    public TraceAreas TraceAreas { get; set; }
    [XmlElement(ElementName = "failureDefinitions")]
    public FailureDefinitions FailureDefinitions { get; set; }
    [XmlElement(ElementName = "areas")]
    public Areas Areas { get; set; }
    [XmlAttribute(AttributeName = "guid")]
    public string Guid { get; set; }
    [XmlAttribute(AttributeName = "image32")]
    public string Image32 { get; set; }
    [XmlAttribute(AttributeName = "lockItem")]
    public string LockItem { get; set; }
    [XmlAttribute(AttributeName = "verb")]
    public string Verb { get; set; }
    [XmlAttribute(AttributeName = "modules")]
    public string Modules { get; set; }
    [XmlAttribute(AttributeName = "scriptProcessor")]
    public string ScriptProcessor { get; set; }
    [XmlAttribute(AttributeName = "responseBufferLimit")]
    public string ResponseBufferLimit { get; set; }
    [XmlAttribute(AttributeName = "resourceType")]
    public string ResourceType { get; set; }
    [XmlAttribute(AttributeName = "requireAccess")]
    public string RequireAccess { get; set; }
    [XmlAttribute(AttributeName = "allowPathInfo")]
    public string AllowPathInfo { get; set; }
}

[XmlRoot(ElementName = "providers")]
public class Providers
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
}

[XmlRoot(ElementName = "configProtectedData")]
public class ConfigProtectedData
{
    [XmlElement(ElementName = "providers")]
    public Providers Providers { get; set; }
}

[XmlRoot(ElementName = "processModel")]
public class ProcessModel
{
    [XmlAttribute(AttributeName = "loadUserProfile")]
    public string LoadUserProfile { get; set; }
    [XmlAttribute(AttributeName = "setProfileEnvironment")]
    public string SetProfileEnvironment { get; set; }
}

[XmlRoot(ElementName = "applicationPoolDefaults")]
public class ApplicationPoolDefaults
{
    [XmlElement(ElementName = "processModel")]
    public ProcessModel ProcessModel { get; set; }
    [XmlAttribute(AttributeName = "managedRuntimeVersion")]
    public string ManagedRuntimeVersion { get; set; }
}

[XmlRoot(ElementName = "applicationPools")]
public class ApplicationPools
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
    [XmlElement(ElementName = "applicationPoolDefaults")]
    public ApplicationPoolDefaults ApplicationPoolDefaults { get; set; }
}

[XmlRoot(ElementName = "listenerAdapters")]
public class ListenerAdapters
{
    [XmlElement(ElementName = "add")]
    public Add Add { get; set; }
}

[XmlRoot(ElementName = "virtualDirectory")]
public class VirtualDirectory
{
    [XmlAttribute(AttributeName = "path")]
    public string Path { get; set; }
    [XmlAttribute(AttributeName = "physicalPath")]
    public string PhysicalPath { get; set; }
}

[XmlRoot(ElementName = "application")]
public class Application
{
    [XmlElement(ElementName = "virtualDirectory")]
    public VirtualDirectory VirtualDirectory { get; set; }
    [XmlAttribute(AttributeName = "path")]
    public string Path { get; set; }
    [XmlAttribute(AttributeName = "applicationPool")]
    public string ApplicationPool { get; set; }
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }
    [XmlAttribute(AttributeName = "groupId")]
    public string GroupId { get; set; }
}

[XmlRoot(ElementName = "binding")]
public class Binding
{
    [XmlAttribute(AttributeName = "protocol")]
    public string Protocol { get; set; }

    [XmlAttribute(AttributeName = "bindingInformation")]
    public string BindingInformation { get; set; }

    public int Port => int.Parse(BindingInformation.Split(':')[1]);

    public string HostName => BindingInformation.Split(':')[2];
}

[XmlRoot(ElementName = "bindings")]
public class Bindings
{
    [XmlElement(ElementName = "binding")]
    public List<Binding> Binding { get; set; }
}

[XmlRoot(ElementName = "site")]
public class Site
{
    [XmlElement(ElementName = "application")]
    public Application Application { get; set; }
    [XmlElement(ElementName = "bindings")]
    public Bindings Bindings { get; set; }
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }
    [XmlAttribute(AttributeName = "id")]
    public string Id { get; set; }
    [XmlAttribute(AttributeName = "serverAutoStart")]
    public bool ServerAutoStart { get; set; }
}

[XmlRoot(ElementName = "logFile")]
public class LogFile
{
    [XmlAttribute(AttributeName = "logFormat")]
    public string LogFormat { get; set; }
    [XmlAttribute(AttributeName = "directory")]
    public string Directory { get; set; }
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
}

[XmlRoot(ElementName = "traceFailedRequestsLogging")]
public class TraceFailedRequestsLogging
{
    [XmlAttribute(AttributeName = "directory")]
    public string Directory { get; set; }
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
    [XmlAttribute(AttributeName = "maxLogFileSizeKB")]
    public string MaxLogFileSizeKB { get; set; }
}

[XmlRoot(ElementName = "siteDefaults")]
public class SiteDefaults
{
    [XmlElement(ElementName = "logFile")]
    public LogFile LogFile { get; set; }
    [XmlElement(ElementName = "traceFailedRequestsLogging")]
    public TraceFailedRequestsLogging TraceFailedRequestsLogging { get; set; }
}

[XmlRoot(ElementName = "applicationDefaults")]
public class ApplicationDefaults
{
    [XmlAttribute(AttributeName = "applicationPool")]
    public string ApplicationPool { get; set; }
}

[XmlRoot(ElementName = "virtualDirectoryDefaults")]
public class VirtualDirectoryDefaults
{
    [XmlAttribute(AttributeName = "allowSubDirConfig")]
    public string AllowSubDirConfig { get; set; }
}

[XmlRoot(ElementName = "sites")]
public class Sites
{
    [XmlElement(ElementName = "site")]
    public List<Site> Site { get; set; }

    [XmlElement(ElementName = "siteDefaults")]
    public SiteDefaults SiteDefaults { get; set; }

    [XmlElement(ElementName = "applicationDefaults")]
    public ApplicationDefaults ApplicationDefaults { get; set; }

    [XmlElement(ElementName = "virtualDirectoryDefaults")]
    public VirtualDirectoryDefaults VirtualDirectoryDefaults { get; set; }
}

[XmlRoot(ElementName = "system.applicationHost")]
public class SystemApplicationHost
{
    [XmlElement(ElementName = "applicationPools")]
    public ApplicationPools ApplicationPools { get; set; }
    [XmlElement(ElementName = "listenerAdapters")]
    public ListenerAdapters ListenerAdapters { get; set; }
    [XmlElement(ElementName = "sites")]
    public Sites Sites { get; set; }
    [XmlElement(ElementName = "webLimits")]
    public string WebLimits { get; set; }
}

[XmlRoot(ElementName = "cache")]
public class Cache
{
    [XmlAttribute(AttributeName = "diskTemplateCacheDirectory")]
    public string DiskTemplateCacheDirectory { get; set; }
}

[XmlRoot(ElementName = "asp")]
public class Asp
{
    [XmlElement(ElementName = "cache")]
    public Cache Cache { get; set; }
    [XmlElement(ElementName = "limits")]
    public string Limits { get; set; }
    [XmlAttribute(AttributeName = "scriptErrorSentToBrowser")]
    public string ScriptErrorSentToBrowser { get; set; }
}

[XmlRoot(ElementName = "caching")]
public class Caching
{
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
    [XmlAttribute(AttributeName = "enableKernelCache")]
    public string EnableKernelCache { get; set; }
}

[XmlRoot(ElementName = "files")]
public class Files
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
}

[XmlRoot(ElementName = "defaultDocument")]
public class DefaultDocument
{
    [XmlElement(ElementName = "files")]
    public Files Files { get; set; }
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
}

[XmlRoot(ElementName = "directoryBrowse")]
public class DirectoryBrowse
{
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
}

[XmlRoot(ElementName = "globalModules")]
public class GlobalModules
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
}

[XmlRoot(ElementName = "scheme")]
public class Scheme
{
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }
    [XmlAttribute(AttributeName = "dll")]
    public string Dll { get; set; }
}

[XmlRoot(ElementName = "dynamicTypes")]
public class DynamicTypes
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
}

[XmlRoot(ElementName = "staticTypes")]
public class StaticTypes
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
}

[XmlRoot(ElementName = "httpCompression")]
public class HttpCompression
{
    [XmlElement(ElementName = "scheme")]
    public Scheme Scheme { get; set; }
    [XmlElement(ElementName = "dynamicTypes")]
    public DynamicTypes DynamicTypes { get; set; }
    [XmlElement(ElementName = "staticTypes")]
    public StaticTypes StaticTypes { get; set; }
    [XmlAttribute(AttributeName = "directory")]
    public string Directory { get; set; }
}

[XmlRoot(ElementName = "error")]
public class Error
{
    [XmlAttribute(AttributeName = "statusCode")]
    public string StatusCode { get; set; }
    [XmlAttribute(AttributeName = "prefixLanguageFilePath")]
    public string PrefixLanguageFilePath { get; set; }
    [XmlAttribute(AttributeName = "path")]
    public string Path { get; set; }
}

[XmlRoot(ElementName = "httpErrors")]
public class HttpErrors
{
    [XmlElement(ElementName = "error")]
    public List<Error> Error { get; set; }
    [XmlAttribute(AttributeName = "lockAttributes")]
    public string LockAttributes { get; set; }
}

[XmlRoot(ElementName = "httpLogging")]
public class HttpLogging
{
    [XmlAttribute(AttributeName = "dontLog")]
    public string DontLog { get; set; }
}

[XmlRoot(ElementName = "customHeaders")]
public class CustomHeaders
{
    [XmlElement(ElementName = "clear")]
    public string Clear { get; set; }
    [XmlElement(ElementName = "add")]
    public Add Add { get; set; }
}

[XmlRoot(ElementName = "redirectHeaders")]
public class RedirectHeaders
{
    [XmlElement(ElementName = "clear")]
    public string Clear { get; set; }
}

[XmlRoot(ElementName = "httpProtocol")]
public class HttpProtocol
{
    [XmlElement(ElementName = "customHeaders")]
    public CustomHeaders CustomHeaders { get; set; }
    [XmlElement(ElementName = "redirectHeaders")]
    public RedirectHeaders RedirectHeaders { get; set; }
}

[XmlRoot(ElementName = "httpRedirect")]
public class HttpRedirect
{
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
}

[XmlRoot(ElementName = "filter")]
public class Filter
{
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }
    [XmlAttribute(AttributeName = "path")]
    public string Path { get; set; }
    [XmlAttribute(AttributeName = "enableCache")]
    public string EnableCache { get; set; }
    [XmlAttribute(AttributeName = "preCondition")]
    public string PreCondition { get; set; }
}

[XmlRoot(ElementName = "isapiFilters")]
public class IsapiFilters
{
    [XmlElement(ElementName = "filter")]
    public List<Filter> Filter { get; set; }
}

[XmlRoot(ElementName = "access")]
public class Access
{
    [XmlAttribute(AttributeName = "sslFlags")]
    public string SslFlags { get; set; }
}

[XmlRoot(ElementName = "applicationDependencies")]
public class ApplicationDependencies
{
    [XmlElement(ElementName = "application")]
    public Application Application { get; set; }
}

[XmlRoot(ElementName = "anonymousAuthentication")]
public class AnonymousAuthentication
{
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
    [XmlAttribute(AttributeName = "userName")]
    public string UserName { get; set; }
}

[XmlRoot(ElementName = "basicAuthentication")]
public class BasicAuthentication
{
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
}

[XmlRoot(ElementName = "clientCertificateMappingAuthentication")]
public class ClientCertificateMappingAuthentication
{
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
}

[XmlRoot(ElementName = "digestAuthentication")]
public class DigestAuthentication
{
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
}

[XmlRoot(ElementName = "iisClientCertificateMappingAuthentication")]
public class IisClientCertificateMappingAuthentication
{
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
}

[XmlRoot(ElementName = "windowsAuthentication")]
public class WindowsAuthentication
{
    [XmlElement(ElementName = "providers")]
    public Providers Providers { get; set; }
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
}

[XmlRoot(ElementName = "authentication")]
public class Authentication
{
    [XmlElement(ElementName = "anonymousAuthentication")]
    public AnonymousAuthentication AnonymousAuthentication { get; set; }
    [XmlElement(ElementName = "basicAuthentication")]
    public BasicAuthentication BasicAuthentication { get; set; }
    [XmlElement(ElementName = "clientCertificateMappingAuthentication")]
    public ClientCertificateMappingAuthentication ClientCertificateMappingAuthentication { get; set; }
    [XmlElement(ElementName = "digestAuthentication")]
    public DigestAuthentication DigestAuthentication { get; set; }
    [XmlElement(ElementName = "iisClientCertificateMappingAuthentication")]
    public IisClientCertificateMappingAuthentication IisClientCertificateMappingAuthentication { get; set; }
    [XmlElement(ElementName = "windowsAuthentication")]
    public WindowsAuthentication WindowsAuthentication { get; set; }
}

[XmlRoot(ElementName = "authorization")]
public class Authorization
{
    [XmlElement(ElementName = "add")]
    public Add Add { get; set; }
}

[XmlRoot(ElementName = "ipSecurity")]
public class IpSecurity
{
    [XmlAttribute(AttributeName = "allowUnlisted")]
    public string AllowUnlisted { get; set; }
}

[XmlRoot(ElementName = "isapiCgiRestriction")]
public class IsapiCgiRestriction
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
    [XmlAttribute(AttributeName = "notListedIsapisAllowed")]
    public string NotListedIsapisAllowed { get; set; }
    [XmlAttribute(AttributeName = "notListedCgisAllowed")]
    public string NotListedCgisAllowed { get; set; }
}

[XmlRoot(ElementName = "fileExtensions")]
public class FileExtensions
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
    [XmlAttribute(AttributeName = "allowUnlisted")]
    public string AllowUnlisted { get; set; }
    [XmlAttribute(AttributeName = "applyToWebDAV")]
    public string ApplyToWebDAV { get; set; }
}

[XmlRoot(ElementName = "verbs")]
public class Verbs
{
    [XmlAttribute(AttributeName = "allowUnlisted")]
    public string AllowUnlisted { get; set; }
    [XmlAttribute(AttributeName = "applyToWebDAV")]
    public string ApplyToWebDAV { get; set; }
}

[XmlRoot(ElementName = "hiddenSegments")]
public class HiddenSegments
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
    [XmlAttribute(AttributeName = "applyToWebDAV")]
    public string ApplyToWebDAV { get; set; }
}

[XmlRoot(ElementName = "requestFiltering")]
public class RequestFiltering
{
    [XmlElement(ElementName = "fileExtensions")]
    public FileExtensions FileExtensions { get; set; }
    [XmlElement(ElementName = "verbs")]
    public Verbs Verbs { get; set; }
    [XmlElement(ElementName = "hiddenSegments")]
    public HiddenSegments HiddenSegments { get; set; }
}

[XmlRoot(ElementName = "security")]
public class Security
{
    [XmlElement(ElementName = "access")]
    public Access Access { get; set; }
    [XmlElement(ElementName = "applicationDependencies")]
    public ApplicationDependencies ApplicationDependencies { get; set; }
    [XmlElement(ElementName = "authentication")]
    public Authentication Authentication { get; set; }
    [XmlElement(ElementName = "authorization")]
    public Authorization Authorization { get; set; }
    [XmlElement(ElementName = "ipSecurity")]
    public IpSecurity IpSecurity { get; set; }
    [XmlElement(ElementName = "isapiCgiRestriction")]
    public IsapiCgiRestriction IsapiCgiRestriction { get; set; }
    [XmlElement(ElementName = "requestFiltering")]
    public RequestFiltering RequestFiltering { get; set; }
}

[XmlRoot(ElementName = "serverSideInclude")]
public class ServerSideInclude
{
    [XmlAttribute(AttributeName = "ssiExecDisable")]
    public string SsiExecDisable { get; set; }
}

[XmlRoot(ElementName = "mimeMap")]
public class MimeMap
{
    [XmlAttribute(AttributeName = "fileExtension")]
    public string FileExtension { get; set; }
    [XmlAttribute(AttributeName = "mimeType")]
    public string MimeType { get; set; }
}

[XmlRoot(ElementName = "staticContent")]
public class StaticContent
{
    [XmlElement(ElementName = "mimeMap")]
    public List<MimeMap> MimeMap { get; set; }
    [XmlAttribute(AttributeName = "lockAttributes")]
    public string LockAttributes { get; set; }
}

[XmlRoot(ElementName = "traceAreas")]
public class TraceAreas
{
    [XmlElement(ElementName = "add")]
    public List<TraceAreaProvider> Add { get; set; }
}

public class TraceAreaProvider
{
    [XmlAttribute(AttributeName = "provider")]
    public string Provider { get; set; }

    [XmlAttribute(AttributeName = "areas")]
    public string Areas { get; set; }

    [XmlAttribute(AttributeName = "verbosity")]
    public string Verbosity { get; set; }
}

[XmlRoot(ElementName = "failureDefinitions")]
public class FailureDefinitions
{
    [XmlAttribute(AttributeName = "statusCodes")]
    public string StatusCodes { get; set; }
}

[XmlRoot(ElementName = "traceFailedRequests")]
public class TraceFailedRequests
{
    [XmlElement(ElementName = "add")]
    public Add Add { get; set; }
}

[XmlRoot(ElementName = "areas")]
public class Areas
{
    [XmlElement(ElementName = "clear")]
    public string Clear { get; set; }
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
}

[XmlRoot(ElementName = "traceProviderDefinitions")]
public class TraceProviderDefinitions
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
}

[XmlRoot(ElementName = "tracing")]
public class Tracing
{
    [XmlElement(ElementName = "traceFailedRequests")]
    public TraceFailedRequests TraceFailedRequests { get; set; }
    [XmlElement(ElementName = "traceProviderDefinitions")]
    public TraceProviderDefinitions TraceProviderDefinitions { get; set; }
}

[XmlRoot(ElementName = "propertyStores")]
public class PropertyStores
{
    [XmlElement(ElementName = "add")]
    public Add Add { get; set; }
}

[XmlRoot(ElementName = "lockStores")]
public class LockStores
{
    [XmlElement(ElementName = "add")]
    public Add Add { get; set; }
}

[XmlRoot(ElementName = "globalSettings")]
public class GlobalSettings
{
    [XmlElement(ElementName = "propertyStores")]
    public PropertyStores PropertyStores { get; set; }
    [XmlElement(ElementName = "lockStores")]
    public LockStores LockStores { get; set; }
}

[XmlRoot(ElementName = "locks")]
public class Locks
{
    [XmlAttribute(AttributeName = "enabled")]
    public string Enabled { get; set; }
    [XmlAttribute(AttributeName = "lockStore")]
    public string LockStore { get; set; }
}

[XmlRoot(ElementName = "authoring")]
public class Authoring
{
    [XmlElement(ElementName = "locks")]
    public Locks Locks { get; set; }
}

[XmlRoot(ElementName = "webdav")]
public class Webdav
{
    [XmlElement(ElementName = "globalSettings")]
    public GlobalSettings GlobalSettings { get; set; }
    [XmlElement(ElementName = "authoring")]
    public Authoring Authoring { get; set; }
    [XmlElement(ElementName = "authoringRules")]
    public string AuthoringRules { get; set; }
}

[XmlRoot(ElementName = "system.webServer")]
public class SystemWebServer
{
    [XmlElement(ElementName = "serverRuntime")]
    public string ServerRuntime { get; set; }
    [XmlElement(ElementName = "asp")]
    public Asp Asp { get; set; }
    [XmlElement(ElementName = "caching")]
    public Caching Caching { get; set; }
    [XmlElement(ElementName = "cgi")]
    public string Cgi { get; set; }
    [XmlElement(ElementName = "defaultDocument")]
    public DefaultDocument DefaultDocument { get; set; }
    [XmlElement(ElementName = "directoryBrowse")]
    public DirectoryBrowse DirectoryBrowse { get; set; }
    [XmlElement(ElementName = "fastCgi")]
    public string FastCgi { get; set; }
    [XmlElement(ElementName = "globalModules")]
    public GlobalModules GlobalModules { get; set; }
    [XmlElement(ElementName = "httpCompression")]
    public HttpCompression HttpCompression { get; set; }
    [XmlElement(ElementName = "httpErrors")]
    public HttpErrors HttpErrors { get; set; }
    [XmlElement(ElementName = "httpLogging")]
    public HttpLogging HttpLogging { get; set; }
    [XmlElement(ElementName = "httpProtocol")]
    public HttpProtocol HttpProtocol { get; set; }
    [XmlElement(ElementName = "httpRedirect")]
    public HttpRedirect HttpRedirect { get; set; }
    [XmlElement(ElementName = "httpTracing")]
    public string HttpTracing { get; set; }
    [XmlElement(ElementName = "isapiFilters")]
    public IsapiFilters IsapiFilters { get; set; }
    [XmlElement(ElementName = "odbcLogging")]
    public string OdbcLogging { get; set; }
    [XmlElement(ElementName = "security")]
    public Security Security { get; set; }
    [XmlElement(ElementName = "serverSideInclude")]
    public ServerSideInclude ServerSideInclude { get; set; }
    [XmlElement(ElementName = "staticContent")]
    public StaticContent StaticContent { get; set; }
    [XmlElement(ElementName = "tracing")]
    public Tracing Tracing { get; set; }
    [XmlElement(ElementName = "urlCompression")]
    public string UrlCompression { get; set; }
    [XmlElement(ElementName = "validation")]
    public string Validation { get; set; }
    [XmlElement(ElementName = "webdav")]
    public Webdav Webdav { get; set; }
    [XmlElement(ElementName = "webSocket")]
    public string WebSocket { get; set; }
    [XmlElement(ElementName = "applicationInitialization")]
    public string ApplicationInitialization { get; set; }
    [XmlElement(ElementName = "modules")]
    public Modules Modules { get; set; }
    [XmlElement(ElementName = "handlers")]
    public Handlers Handlers { get; set; }
}

[XmlRoot(ElementName = "modules")]
public class Modules
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
}

[XmlRoot(ElementName = "handlers")]
public class Handlers
{
    [XmlElement(ElementName = "add")]
    public List<Add> Add { get; set; }
    [XmlAttribute(AttributeName = "accessPolicy")]
    public string AccessPolicy { get; set; }
}

[XmlRoot(ElementName = "location")]
public class Location
{
    [XmlElement(ElementName = "system.webServer")]
    public SystemWebServer SystemWebServer { get; set; }
    [XmlAttribute(AttributeName = "path")]
    public string Path { get; set; }
    [XmlAttribute(AttributeName = "overrideMode")]
    public string OverrideMode { get; set; }
}

[XmlRoot(ElementName = "configuration")]
public class ApplicationHostConfiguration
{
    [XmlElement(ElementName = "configSections")]
    public ConfigSections ConfigSections { get; set; }
    [XmlElement(ElementName = "configProtectedData")]

    public ConfigProtectedData ConfigProtectedData { get; set; }
    [XmlElement(ElementName = "system.applicationHost")]

    public SystemApplicationHost SystemApplicationHost { get; set; }
    [XmlElement(ElementName = "system.webServer")]

    public SystemWebServer SystemWebServer { get; set; }

    [XmlElement(ElementName = "location")]
    public Location Location { get; set; }
}
