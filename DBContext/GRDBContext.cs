using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MXAccess_RestAPI.DBContext;

public partial class GRDBContext : DbContext
{
    public GRDBContext()
    {
    }

    public GRDBContext(DbContextOptions<GRDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Gobject> Gobjects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<Gobject>(entity =>
        {
            entity.ToTable("gobject", tb =>
                {
                    tb.HasTrigger("trigger_delete_gobject");
                    tb.HasTrigger("trigger_insert_gobject");
                    tb.HasTrigger("trigger_update_gobject");
                });

            entity.HasIndex(e => e.CheckedOutPackageId, "idx_gobject_checked_out_package_id");

            entity.HasIndex(e => new { e.TagName, e.NamespaceId, e.DeployedPackageId, e.CheckedInPackageId, e.CheckedOutPackageId, e.ContainedName, e.ContainedByGobjectId }, "idx_gobject_multi");

            entity.HasIndex(e => e.AreaGobjectId, "idx_gobject_on_area_gobject_id");

            entity.HasIndex(e => e.CheckedInPackageId, "idx_gobject_on_checked_in_package_id");

            entity.HasIndex(e => e.ContainedByGobjectId, "idx_gobject_on_contained_by_gobject_id");

            entity.HasIndex(e => e.DeployedPackageId, "idx_gobject_on_deployed_package_id");

            entity.HasIndex(e => e.DerivedFromGobjectId, "idx_gobject_on_derived_from_gobject_id");

            entity.HasIndex(e => new { e.GobjectId, e.CheckedInPackageId, e.CheckedOutPackageId }, "idx_gobject_on_gobject_id_and_checked_in_package_id_and_checked_out_package_id");

            entity.HasIndex(e => e.HostedByGobjectId, "idx_gobject_on_hosted_by_gobject_id");

            entity.HasIndex(e => new { e.LastDeployedPackageId, e.DeployedPackageId }, "idx_gobject_on_last_deployed_package_id_and_deployed_package_id");

            entity.HasIndex(e => new { e.TagName, e.NamespaceId }, "idx_gobject_on_tag_name_and_namespace_id");

            entity.HasIndex(e => e.TemplateDefinitionId, "idx_gobject_on_template_definition_id");

            entity.HasIndex(e => new { e.ContainedByGobjectId, e.ContainedName, e.TagName }, "idx_gobject_on_various_containment");

            entity.Property(e => e.GobjectId).HasColumnName("gobject_id");
            entity.Property(e => e.AreaGobjectId).HasColumnName("area_gobject_id");
            entity.Property(e => e.CheckedInPackageId).HasColumnName("checked_in_package_id");
            entity.Property(e => e.CheckedOutByUserGuid).HasColumnName("checked_out_by_user_guid");
            entity.Property(e => e.CheckedOutPackageId).HasColumnName("checked_out_package_id");
            entity.Property(e => e.ConfigurationGuid).HasColumnName("configuration_guid");
            entity.Property(e => e.ConfigurationVersion).HasColumnName("configuration_version");
            entity.Property(e => e.ContainedByGobjectId).HasColumnName("contained_by_gobject_id");
            entity.Property(e => e.ContainedName)
                .HasMaxLength(32)
                .HasColumnName("contained_name");
            entity.Property(e => e.DefaultDisplayGobjectId).HasColumnName("default_display_gobject_id");
            entity.Property(e => e.DefaultSymbolGobjectId).HasColumnName("default_symbol_gobject_id");
            entity.Property(e => e.DeployedPackageId).HasColumnName("deployed_package_id");
            entity.Property(e => e.DeployedVersion).HasColumnName("deployed_version");
            entity.Property(e => e.DeploymentPendingStatus).HasColumnName("deployment_pending_status");
            entity.Property(e => e.DerivedFromGobjectId).HasColumnName("derived_from_gobject_id");
            entity.Property(e => e.HierarchicalName)
                .HasMaxLength(329)
                .HasColumnName("hierarchical_name");
            entity.Property(e => e.HostedByGobjectId).HasColumnName("hosted_by_gobject_id");
            entity.Property(e => e.HostingTreeLevel).HasColumnName("hosting_tree_level");
            entity.Property(e => e.IdentityGuid).HasColumnName("identity_guid");
            entity.Property(e => e.IsHidden).HasColumnName("is_hidden");
            entity.Property(e => e.IsTemplate).HasColumnName("is_template");
            entity.Property(e => e.LastDeployedPackageId).HasColumnName("last_deployed_package_id");
            entity.Property(e => e.NamespaceId).HasColumnName("namespace_id");
            entity.Property(e => e.SoftwareUpgradeNeeded).HasColumnName("software_upgrade_needed");
            entity.Property(e => e.TagName)
                .HasMaxLength(329)
                .HasColumnName("tag_name");
            entity.Property(e => e.TemplateDefinitionId).HasColumnName("template_definition_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public List<string> GetRuntimeObjectInstances()
    {
        return Gobjects
                .Where(g => !g.IsTemplate && g.NamespaceId == 1)
                .Select(g => g.TagName)
                .ToList();
    }
}
