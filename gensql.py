
import random

words = [ "First", "Second", "Third", "Fourth", "Fith", "Sixth", "Type", "Types", "Category", "Categories" ]
types = [ "int", "nvarchar(max)", "nvarchar(5)", "bit", "bigint", "smallint", "tinyint", "text", "float", "date", "money" ]

procs = set()
while len(procs) < 5000:
    name = "_".join([ words[random.randrange(0, len(words))] for level in range(1, random.randrange(2, 8)) ])
    if random.randrange(0, 2) == 1:
        procs.add(name)
    else:
        procs.add("p_" + name)

tables = set()
while len(tables) < 300:
    name = "".join([ words[random.randrange(0, len(words))] for level in range(1, random.randrange(2, 8)) ])
    tables.add(name)

f = open("out.sql", "w")
for p in procs:
    f.write("create procedure ")
    f.write(p)
    f.write("\n")

    for x in range(0, random.randrange(0, len(words))):
        if x != 0:
            f.write(", ")
        else:
            f.write("  ")

        f.write("@")
        f.write(words[x + 1])
        f.write(" ")
        f.write(types[random.randrange(0, len(types))])
        f.write("\n")

    f.write("as\nselect 1\ngo\n\n")

for t in tables:
    f.write("create table ")
    f.write(t)
    f.write("(\n")

    for x in range(0, random.randrange(1, len(words))):
        if x != 0:
            f.write(", ")
        else:
            f.write("  ")

        f.write("[")
        f.write(words[x + 1])
        f.write("] ")
        if x == 0 and random.randrange(0, 2) == 1:
            f.write("int identity(1,1) not null")
        else:
            f.write(types[random.randrange(0, len(types))])

        f.write("\n")

    f.write(")\ngo\n\n")

f.close()