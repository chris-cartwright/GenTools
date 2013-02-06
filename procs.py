
import random

words = [ "First", "Second", "Third", "Fourth", "Fith", "Sixth" ]
types = [ "int", "nvarchar(max)", "nvarchar(5)", "bit", "bigint", "smallint" ]

procs = set()
while(len(procs) < 200):
    name = "_".join([ words[random.randrange(0, len(words))] for level in range(1, random.randrange(2, 8)) ])
    if random.randrange(0, 2) == 1:
        procs.add(name)
    else:
        procs.add("p_" + name)

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

f.close()