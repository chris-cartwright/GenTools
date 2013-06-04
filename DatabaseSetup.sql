
if exists (
  select *
  from sys.objects
  where object_id = object_id(N'p_ListTables')
    and [type] in (N'P', N'PC')
)
drop procedure p_ListTables

go

create procedure p_ListTables
as

select
  t.name as 'table',
  c.name as 'column',
  c.column_id as 'priority',
  ty.name as 'type',
  c.is_nullable as 'nullable',
  c.is_identity as 'identity'
from sys.tables  t
join sys.all_columns c
  on t.object_id = c.object_id
join sys.types ty
  on c.user_type_id = ty.user_type_id
where t.[type] = 'U'
order by t.name, c.column_id

go

if exists (
  select *
  from sys.objects
  where object_id = object_id(N'p_ListTypeTables')
    and [type] in (N'P', N'PC')
)
drop procedure p_ListTypeTables

go

create procedure p_ListTypeTables
as

;with table_cte (name, object_id, [type])
as (
  select t.name, t.object_id, t.[type]
  from sys.tables t
  join sys.all_columns c
    on t.object_id = c.object_id and c.is_identity = 1
)
select
  t.name as 'table',
  c.name as 'column',
  c.column_id as 'priority',
  ty.name as 'type',
  c.is_nullable as 'nullable',
  c.is_identity as 'identity'
from table_cte t
join sys.all_columns c
  on t.object_id = c.object_id
join sys.types ty
  on c.user_type_id = ty.user_type_id
where t.[type] = 'U'
  and (
    t.name like '%Type'
    or t.name like '%Types'
    or t.name like '%Category'
    or t.name like '%Categories'
  )
order by t.name, c.column_id

go

if exists (
  select *
  from sys.objects
  where object_id = object_id(N'p_ListProcedures')
    and [type] in (N'P', N'PC')
)
drop procedure p_ListProcedures

go

create procedure p_ListProcedures
as

select
  sp.name as 'procedure',
  p.name as 'parameter',
  t.name as 'type',
  p.parameter_id as 'order',
  -- http://msdn.microsoft.com/en-us/library/ms176089.aspx
  case when p.max_length=-1 then 2147483647 else p.max_length end as 'size',
  p.is_output as 'output',
  dbo.GetParamDefault(p.name, object_definition(sp.object_id)) as 'value'
from sys.procedures sp
left join sys.parameters p
  on p.object_id=sp.object_id
left join sys.types t
  on t.user_type_id=p.user_type_id
order by sp.name, p.parameter_id

go

if exists (
  select *
  from sys.objects
  where object_id = object_id(N'GetParamDefault')
    and [type] in (N'FN')
)
drop function GetParamDefault

go

-- Modified code from http://www.codeproject.com/Articles/12939/Figure-Out-the-Default-Values-of-Stored-Procedure
create function GetParamDefault (
  @ParamName varchar(50)
, @Text varchar(max)
)
returns varchar(100)
as
begin

declare @StartPos int
declare @EndPos int
declare @DefaultValue varchar(50)

select @StartPos = patindex('%' + @ParamName + '%', @Text)
if @StartPos<>0 begin
  select @text = right(@Text, len(@Text) - (@StartPos + 1))
  -- find the end of a line
  select @endPos = charindex(char(10), @Text)
  select @text = left(@Text, @EndPos - 1)
  select @StartPos = patindex('%=%', @Text)

  if @StartPos <> 0 begin
    select @DefaultValue = ltrim(rtrim(right(@Text, len(@Text) - @StartPos)))
    select @EndPos = charindex('--', @DefaultValue)
    if @EndPos <> 0 begin
      select @DefaultValue = rtrim(left(@DefaultValue, @EndPos - 1))
    end

    select @EndPos = charindex(',', @DefaultValue)
    if @EndPos <> 0 begin
      select @DefaultValue = rtrim(left(@DefaultValue, @EndPos - 1))
    end

    select @EndPos = patindex('%output%', @DefaultValue)
    if @EndPos <> 0 begin
      select @DefaultValue = rtrim(left(@DefaultValue, @EndPos - 1))
    end

    select @EndPos = charindex('/*', @DefaultValue)
    if @EndPos <> 0 begin
      select @DefaultValue = rtrim(left(@DefaultValue, @EndPos - 1))
    end

    return @DefaultValue
  end
end

return null

end

go