
declare @name varchar(500)

-- Delete stored procedures
declare procs cursor
for
  select [name]
  from sys.procedures
  where [type]='P'
    and is_ms_shipped=0

open procs
fetch next from procs into @name

while @@fetch_status = 0 begin
    exec('drop procedure ' + @name)

    fetch next from procs into @name
end

close procs
deallocate procs

-- Delete tables
declare tbls cursor
for
  select [name]
  from sys.tables
  where type_desc='USER_TABLE'

open tbls
fetch next from tbls into @name

while @@fetch_status = 0 begin
    exec('drop table ' + @name)

    fetch next from tbls into @name
end

close tbls
deallocate tbls

-- Delete table types
declare tts cursor
for
	select name
	from sys.table_types

open tts
fetch next from tts into @name

while @@fetch_status = 0 begin
	exec('drop type ' + @name)

	fetch next from tts into @name
end

close tts
deallocate tts