using Npgsql;

const string connStr =
    "Host=dpg-d80vvhvavr4c73aulnp0-a.oregon-postgres.render.com;Port=5432;Database=virtualmed_3vq4;Username=admin;Password=KNbBik3HdNJHm73bLBIp4o8i3iU92ewu;SSL Mode=Require;Trust Server Certificate=true";

await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
Console.WriteLine("Conectado a PostgreSQL");

await using (var cmd = new NpgsqlCommand(
    "SELECT \"Id\", \"FileName\", \"Status\" FROM rag_documents WHERE \"NormalizedFileName\" = @name",
    conn))
{
    cmd.Parameters.AddWithValue("name", "guia-tratamiento-ansiedad-generalizada.pdf");
    await using var reader = await cmd.ExecuteReaderAsync();
    var count = 0;
    while (await reader.ReadAsync())
    {
        count++;
        Console.WriteLine($"Existente: {reader.GetGuid(0)} | {reader.GetString(1)} | status={reader.GetInt32(2)}");
    }
    if (count == 0) Console.WriteLine("No hay duplicado en rag_documents");
}

await using (var dup = new NpgsqlCommand(
    "SELECT EXISTS(SELECT 1 FROM rag_documents WHERE \"NormalizedFileName\" = @name)",
    conn))
{
    dup.Parameters.AddWithValue("name", "guia-tratamiento-ansiedad-generalizada.pdf");
    var exists = (bool)(await dup.ExecuteScalarAsync() ?? false);
    Console.WriteLine($"Duplicate exists: {exists}");
}

await using (var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM rag_documents", conn))
{
    var total = (long)(await countCmd.ExecuteScalarAsync() ?? 0L);
    Console.WriteLine($"Total rag_documents: {total}");
}
await using (var userCmd = new NpgsqlCommand(
    "SELECT u.\"Id\", u.\"Email\" FROM users u JOIN roles r ON u.\"RoleId\" = r.\"Id\" WHERE r.\"Name\" = 'Admin' LIMIT 1",
    conn))
await using (var users = await userCmd.ExecuteReaderAsync())
{
    if (await users.ReadAsync())
    {
        var adminId = users.GetGuid(0);
        var email = users.GetString(1);
        Console.WriteLine($"Admin: {adminId} | {email}");

        await users.CloseAsync();

        var docId = Guid.NewGuid();
        await using var insert = new NpgsqlCommand(@"
            SELECT set_config('app.user_id', @userId, false);
            INSERT INTO rag_documents (""Id"", ""FileName"", ""NormalizedFileName"", ""StorageKey"", ""FileSizeBytes"", ""Status"", ""UploadedByUserId"", ""CreatedAt"")
            VALUES (@id, @fileName, @norm, @key, @size, 0, @userGuid, NOW());
            DELETE FROM rag_documents WHERE ""Id"" = @id;", conn);
        insert.Parameters.AddWithValue("userId", adminId.ToString());
        insert.Parameters.AddWithValue("id", docId);
        insert.Parameters.AddWithValue("fileName", "test-insert.pdf");
        insert.Parameters.AddWithValue("norm", "test-insert.pdf");
        insert.Parameters.AddWithValue("key", $"{docId:N}/test-insert.pdf");
        insert.Parameters.AddWithValue("size", 1234L);
        insert.Parameters.AddWithValue("userGuid", adminId);
        await insert.ExecuteNonQueryAsync();
        Console.WriteLine("Insert + delete test OK");
    }
}
