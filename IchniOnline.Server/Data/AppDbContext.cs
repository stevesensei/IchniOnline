using System.Text.Json;
using IchniOnline.Server.Entities;
using IchniOnline.Server.Entities.ThirdParty;
using IchniOnline.Server.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IchniOnline.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<GameUser> GameUsers => Set<GameUser>();
    public DbSet<BeatmapDb> Beatmaps => Set<BeatmapDb>();
    public DbSet<PlayData> PlayDataRecords => Set<PlayData>();
    public DbSet<TapTapOauth> TapTapOauthRecords => Set<TapTapOauth>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var notesConverter = new ValueConverter<List<BeatmapNoteDto>, string>(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<List<BeatmapNoteDto>>(value, (JsonSerializerOptions?)null) ?? new List<BeatmapNoteDto>());

        var notesComparer = new ValueComparer<List<BeatmapNoteDto>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
            value => JsonSerializer.Deserialize<List<BeatmapNoteDto>>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new List<BeatmapNoteDto>());

        modelBuilder.Entity<GameUser>(entity =>
        {
            entity.ToTable("game_user");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(100);
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100);
            entity.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(300);
            entity.Property(x => x.Permission).HasColumnName("permission");
            entity.Property(x => x.PasswordHashed).HasColumnName("password_hashed").HasMaxLength(200);
        });

        modelBuilder.Entity<BeatmapDb>(entity =>
        {
            entity.ToTable("beatmap");
            entity.HasKey(x => x.BeatmapId);
            entity.Property(x => x.BeatmapId).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.CollectionId).HasColumnName("collection_id");
            entity.Property(x => x.SongName).HasColumnName("song_name").HasMaxLength(100);
            entity.Property(x => x.IllustrateUrl).HasColumnName("illustrate_url").HasMaxLength(100);
            entity.Property(x => x.Illustrator).HasColumnName("illustrator").HasMaxLength(100);
            entity.Property(x => x.Composer).HasColumnName("composer").HasMaxLength(100);
            entity.Property(x => x.LevelDesigner).HasColumnName("level_designer").HasMaxLength(100);
            entity.Property(x => x.Difficulty).HasColumnName("difficulty").HasMaxLength(100);
            entity.Property(x => x.LevelColor).HasColumnName("level_color").HasMaxLength(100);
            entity.Property(x => x.Notes)
                .HasColumnName("notes")
                .HasColumnType("jsonb")
                .HasConversion(notesConverter)
                .Metadata.SetValueComparer(notesComparer);
            entity.Property(x => x.Version).HasColumnName("version");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.ScheduledReleaseTime).HasColumnName("release_time");
        });

        modelBuilder.Entity<PlayData>(entity =>
        {
            entity.ToTable("play_data");
            entity.HasKey(x => x.PlayDataId);
            entity.Property(x => x.PlayDataId).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.BeatmapId).HasColumnName("beatmap_id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.PerfectCount).HasColumnName("perfect_count");
            entity.Property(x => x.GoodCount).HasColumnName("good_count");
            entity.Property(x => x.BadCount).HasColumnName("bad_count");
            entity.Property(x => x.MissCount).HasColumnName("miss_count");
            entity.Property(x => x.MaxCombo).HasColumnName("max_combo");
            entity.Property(x => x.AchieveTime).HasColumnName("time");
            entity.Property(x => x.IsValid).HasColumnName("is_valid");

            entity.HasOne(x => x.Beatmap)
                .WithMany()
                .HasForeignKey(x => x.BeatmapId);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<TapTapOauth>(entity =>
        {
            entity.ToTable("taptap_inter_auth");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.BindUserId).HasColumnName("user_id");
            entity.Property(x => x.TapTapOpenId).HasColumnName("taptap_open_id").HasMaxLength(100);
            entity.Property(x => x.TapTapUnionId).HasColumnName("taptap_union_id").HasMaxLength(100);
            entity.Property(x => x.Status).HasColumnName("status");
        });
    }
}

