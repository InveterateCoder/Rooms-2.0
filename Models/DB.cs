using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Rooms.Models
{
    public class RoomsDBContext : DbContext
    {
        public RoomsDBContext(DbContextOptions<RoomsDBContext> opts) : base(opts) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<RegQueueEntity> RegQueue { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Room>()
                .HasIndex(r => r.Slug)
                .IsUnique();
            modelBuilder.Entity<RegQueueEntity>()
                .HasIndex(e => e.Key)
                .IsUnique();
        }
    }
    public class User
    {
        public long UserId { get; set; }
        [Required, MaxLength(320)]
        public string Email { get; set; }
        [Required, MaxLength(34)]
        public string Name { get; set; }
        [Required, MaxLength(16)]
        public string Password { get; set; }
        public Room Room { get; set; }
    }
    public class Room
    {
        public long RoomId { get; set; }
        [Required]
        public long UserId { get; set; }
        public User User { get; set; }
        [Required, MaxLength(2)]
        public string Country { get; set; }
        [Required, MaxLength(34)]
        public string Name { get; set; }
        [Required, MaxLength(34)]
        public string Slug { get; set; }
        [MaxLength(200)]
        public string Description { get; set; }
        [MaxLength(16)]
        public string Password { get; set; }
        [Required]
        public byte Limit { get; set; }
        public IEnumerable<Message> Messages { get; set; }
    }
    public class Message
    {
        public long MessageId { get; set; }
        [Required]
        public long RoomId { get; set; }
        [Required]
        public Room Room { get; set; }
        [Required]
        public long TimeStamp { get; set; }
        public long UserId { get; set; }
        [MaxLength(40)]
        public string GUID { get; set; }
        [MaxLength(5000)]
        public string AccessIdsJson { get; set; }
        [NotMapped]
        public IEnumerable<long> AccessIds
        {
            get => AccessIdsJson == null ? null : JsonSerializer.Deserialize<IEnumerable<long>>(AccessIdsJson);
            set => AccessIdsJson = value == null ? null : JsonSerializer.Serialize<IEnumerable<long>>(value);
        }
        [Required, MaxLength(34)]
        public string SenderName { get; set; }
        [Required, MaxLength(5)]
        public string SenderIcon { get; set; }
        [Required, MaxLength(2000)]
        public string Text { get; set; }
    }
    public class RegQueueEntity
    {
        public int Id { get; set; }
        [Required]
        public long Date { get; set; }
        [Required, MaxLength(10)]
        public string Key { get; set; }
        [Required, MaxLength(34)]
        public string Name { get; set; }
        [Required, MaxLength(320)]
        public string Email { get; set; }
        [MaxLength(16)]
        public string Password { get; set; }
    }
}