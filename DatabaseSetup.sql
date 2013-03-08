
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
order by t.name

go

create procedure p_ListProcedures
as

select
  sp.name as 'procedure',
  p.name as 'parameter',
  t.name as 'type',
  p.is_output as 'output',
  dbo.GetParamDefault(p.name, object_definition(sp.object_id)) as 'value'
from sys.procedures sp
left join sys.parameters p
  on p.object_id=sp.object_id
left join sys.types t
  on t.user_type_id=p.user_type_id
order by sp.name

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
if @StartPos<>0
begin
  select @text = right(@Text, len(@Text) - (@StartPos + 1))
  -- find the end of a line
  select @endPos = charindex(char(10), @Text)
  select @text = left(@Text, @EndPos - 1)
  select @StartPos = patindex('%=%', @Text)

  if @StartPos <> 0
  begin
    select @DefaultValue = ltrim(rtrim(right(@Text, len(@Text) - @StartPos)))
    select @EndPos = charindex('--', @DefaultValue)
    if @EndPos <> 0
      select @DefaultValue = rtrim(left(@DefaultValue, @EndPos - 1))

    select @EndPos = charindex(',', @DefaultValue)
    if @EndPos <> 0
      select @DefaultValue = rtrim(left(@DefaultValue, @EndPos - 1))

    select @EndPos = patindex('%output%', @DefaultValue)
    if @EndPos <> 0
      select @DefaultValue = rtrim(left(@DefaultValue, @EndPos - 1))

    return @DefaultValue
  end
end

return null

end

go