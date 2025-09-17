using Cassandra;
using Npgsql;
using AppSettings;
using System.Text.Json;
using DB.PostgreSQL;

namespace DB.ScyllaDB;


public class Scylla
{
    internal BucketCache buckets;
    internal PgBuckets bucketDB = new();
    protected Dictionary<string, object[]> types = [];

    public Scylla()
    {
        buckets = new(ref bucketDB);
        DefineType();
    }

    private void DefineType()
    {
        string[] hosts = ParseArray((JsonElement)Settings.GetJSON("scyllaTestHosts")!);
        Cluster testCluster = Cluster.Builder().AddContactPoints(hosts).Build();

        hosts = ParseArray((JsonElement)Settings.GetJSON("scyllaPostsHosts")!);
        Cluster postsCluster = Cluster.Builder().AddContactPoints(hosts).Build();

        types = new()
        {                   // subType / table name / cluster name / keyspace name
            { "test_single", ["single", "test_single", testCluster, "test_keyspace"] },
            { "test_double", ["double", "test_double", testCluster, "test_keyspace"] },
            { "post_comments", ["single", "comments", postsCluster, "post_comments"] }
        };
    }

    public void IncrementBucketCounter(byte[] bucket_id)
    {
        string queryText = "UPDATE buckets SET counter = counter + 1 WHERE bucket_id = $1";
        bucketDB.Execute(queryText, [bucket_id]);
    }

    public void TriggerBucketChange(byte[] bucket_id, DateTime nextChange)
    {
        string queryText = "UPDATE buckets SET bucket = bucket + 1, next_change = $1, last_change = $1 WHERE bucket_id = $2";
        bucketDB.Execute(queryText, [nextChange, bucket_id]);
    }

    public void ResetBucketCounter(byte[] bucket_id)
    {
        string queryText = "UPDATE buckets SET next_change = $1, counter = 1 WHERE bucket_id = $2";
        bucketDB.Execute(queryText, [DBNull.Value, bucket_id]);
    }

    internal static string[] ParseArray(JsonElement JSON)
    {
        //JsonElement root = JSON.RootElement;
        return JsonSerializer.Deserialize<string[]>(JSON)!;
    }


    // Caches [int bucket, DateTime next_change, DateTime last_change, int counter] for buckets
    internal class BucketCache
    {
        private Dictionary<byte[], LRUNode> bucketCache;
        private int capacity;
        LRUNode head;
        LRUNode tail;
        PgBuckets bucketDB;

        public BucketCache(ref PgBuckets db, int size = 100)
        {
            bucketDB = db;
            capacity = size;
            bucketCache = new(size);
            head = new LRUNode([], []);
            tail = new LRUNode([], []);
            head.next = tail;
            tail.prev = head;
        }

        private void MoveToEnd(LRUNode node)
        {
            Remove(node);
            AddToEnd(node);
        }

        private void Remove(LRUNode node)
        {
            node.next!.prev = node.prev;
            node.prev!.next = node.next;
        }

        private void AddToEnd(LRUNode node)
        {
            node.next = tail;
            node.prev = tail.prev;
            tail.prev!.next = node;
            tail.prev = node;
        }

        private object[] CreateBucket(byte[] bucket_id)
        {
            string queryText = "INSERT INTO buckets (bucket_id, bucket, next_change, last_change, counter) VALUES ($1, $2, $3, $4, $5)";
            object[] parameters = [bucket_id, 0, DBNull.Value, DateTime.UtcNow, 0];
            bucketDB.Execute(queryText, parameters);

            return parameters;
        }

        // Returns array with the follow data: [int bucket, DateTime next_change, DateTime last_change, int counter]
        public object[] GetBucketData(byte[] bucket_id)
        {
            if (!bucketCache.ContainsKey(bucket_id)) UpdateBucket(bucket_id);
            var node = bucketCache[bucket_id];

            // Refresh bucket cache if current information is older than 5 hours
            if ((DateTime)node.value[4] < DateTime.UtcNow.AddHours(-5))
            {
                UpdateBucket(bucket_id);
                node = bucketCache[bucket_id];
            }
            else
            {
                MoveToEnd(node);
            }

            return node.value;
        }

        // Inserts/Updates an entry in the bucket cache dictionary
        private void UpsertBucketCache(byte[] bucket_id, object[] value)
        {
            if (!bucketCache.ContainsKey(bucket_id))
            {
                while (bucketCache.Count >= capacity)
                {
                    LRUNode first = head.next!;
                    bucketCache.Remove(first.key);
                    Remove(first);
                }
                LRUNode newNode = new(bucket_id, value);
                bucketCache[bucket_id] = newNode;
                AddToEnd(newNode);
            }
            else
            {
                LRUNode node = bucketCache[bucket_id];
                MoveToEnd(node);
                node.value = value;
            }
        }

        // Updates bucket cache with the latest from the bucket DB; creates DB entry if it doesn't already exist
        public void UpdateBucket(byte[] bucket_id)
        {
            string queryText = "SELECT bucket, next_change, last_change, counter FROM buckets WHERE bucket_id = $1";
            NpgsqlDataReader result = bucketDB.Read(queryText, [bucket_id]);

            if (result.HasRows)
            {
                result.Read();
                object[] value = [result.GetFieldValue<int>(0), result.GetFieldValue<DateTime?>(1)!, result.GetFieldValue<DateTime>(2), result.GetFieldValue<int>(3), DateTime.UtcNow];
                UpsertBucketCache(bucket_id, value);
            }
            else
            {
                object[] value = CreateBucket(bucket_id);
                value[0] = value[1]; value[1] = value[2]; value[2] = value[3]; value[3] = value[4];
                value[4] = DateTime.UtcNow;
                UpsertBucketCache(bucket_id, value);
            }
        }


        private class LRUNode
        {
            public byte[] key;
            public object[] value;
            public LRUNode? prev;
            public LRUNode? next;

            public LRUNode(byte[] keyArg, object[] valueArg)
            {
                key = keyArg;
                value = valueArg;
            }
        }
    }
}

