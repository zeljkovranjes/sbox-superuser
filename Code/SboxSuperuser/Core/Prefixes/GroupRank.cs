namespace SboxSuperuser.Core.Prefixes;

/// <summary>A group a player belongs to, with its protection level; the highest level picks the group prefix.</summary>
internal readonly record struct GroupRank( string GroupId, int Protection );
