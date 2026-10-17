using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Nova.Core
{
    public sealed class TableRecord
    {
        public string TableId { get; set; }
        public long HighScore { get; set; }
        public int LongestMultiballSeconds { get; set; }
    }

    public sealed class RecordBook
    {
        public const int SchemaVersion = 1;
        public List<TableRecord> Records { get; set; } = new List<TableRecord>();
        public bool Tampered { get; private set; }

        sealed class Envelope
        {
            public int SchemaVersion { get; set; }
            public string Payload { get; set; }
            public string Checksum { get; set; }
        }

        public void Record(string tableId, long score, int multiballSeconds)
        {
            if (string.IsNullOrWhiteSpace(tableId) || score < 0 || multiballSeconds < 0)
                throw new ArgumentException("Invalid record");
            var entry = Records.Find(r => r.TableId == tableId);
            if (entry == null)
            {
                entry = new TableRecord { TableId = tableId };
                Records.Add(entry);
            }
            entry.HighScore = Math.Max(entry.HighScore, score);
            entry.LongestMultiballSeconds = Math.Max(entry.LongestMultiballSeconds, multiballSeconds);
        }

        static string Digest(int version, string payload)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(version + ":" + payload));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) result.Append(b.ToString("x2"));
                return result.ToString();
            }
        }

        public string ToJson()
        {
            var payload = JsonConvert.SerializeObject(Records);
            return JsonConvert.SerializeObject(new Envelope
            {
                SchemaVersion = SchemaVersion,
                Payload = payload,
                Checksum = Digest(SchemaVersion, payload)
            });
        }

        public static RecordBook FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Missing record book", nameof(json));
            var envelope = JsonConvert.DeserializeObject<Envelope>(json);
            if (envelope == null || envelope.SchemaVersion != SchemaVersion || envelope.Payload == null || envelope.Checksum == null)
                throw new FormatException("Unsupported or incomplete record book");
            // Flag tampering without silently deleting readable records. Persist only after explicit user action.
            var book = new RecordBook { Tampered = !string.Equals(envelope.Checksum,
                Digest(envelope.SchemaVersion, envelope.Payload), StringComparison.OrdinalIgnoreCase) };
            book.Records = JsonConvert.DeserializeObject<List<TableRecord>>(envelope.Payload)
                ?? throw new FormatException("Missing records");
            foreach (var entry in book.Records)
                if (entry == null || string.IsNullOrWhiteSpace(entry.TableId) || entry.HighScore < 0 || entry.LongestMultiballSeconds < 0)
                    throw new FormatException("Invalid record");
            return book;
        }
    }
}
