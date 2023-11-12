using System;
using System.Collections.Generic;

namespace MXAccess_RestAPI.DBContext;

public partial class Gobject
{
    public int GobjectId { get; set; }

    public int TemplateDefinitionId { get; set; }

    public int DerivedFromGobjectId { get; set; }

    public int ContainedByGobjectId { get; set; }

    public int AreaGobjectId { get; set; }

    public int HostedByGobjectId { get; set; }

    public Guid? CheckedOutByUserGuid { get; set; }

    public int DefaultSymbolGobjectId { get; set; }

    public int DefaultDisplayGobjectId { get; set; }

    public int CheckedInPackageId { get; set; }

    public int CheckedOutPackageId { get; set; }

    public int DeployedPackageId { get; set; }

    public int LastDeployedPackageId { get; set; }

    public string TagName { get; set; } = null!;

    public string ContainedName { get; set; } = null!;

    public Guid IdentityGuid { get; set; }

    public Guid ConfigurationGuid { get; set; }

    public int ConfigurationVersion { get; set; }

    public int DeployedVersion { get; set; }

    public bool IsTemplate { get; set; }

    public bool IsHidden { get; set; }

    public bool SoftwareUpgradeNeeded { get; set; }

    public short HostingTreeLevel { get; set; }

    public string HierarchicalName { get; set; } = null!;

    public short NamespaceId { get; set; }

    public bool DeploymentPendingStatus { get; set; }
}
