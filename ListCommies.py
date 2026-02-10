#!/usr/bin/env python3
import re
from tqdm import tqdm

# If anyone can think of a more elegant way of doing this, I'd love to hear about it.
done = set()
with open("answers.txt", "r") as fin, open("commies.txt", "w") as fout:
    for line in tqdm(fin, mininterval=1, leave=False, colour="green"):
        line = line.rstrip("\r\n")
        if line in done:
            continue
        commies = set()
        if m := re.search(r"^([0-9]+)\+([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}+{m.group(2)}={m.group(3)}")
            commies.add(f"{m.group(2)}+{m.group(1)}={m.group(3)}")
        elif m := re.search(r"^([0-9]+)\+([0-9]+)\+([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}+{m.group(2)}+{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(1)}+{m.group(3)}+{m.group(2)}={m.group(4)}")
            commies.add(f"{m.group(2)}+{m.group(1)}+{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(2)}+{m.group(3)}+{m.group(1)}={m.group(4)}")
            commies.add(f"{m.group(3)}+{m.group(1)}+{m.group(2)}={m.group(4)}")
            commies.add(f"{m.group(3)}+{m.group(2)}+{m.group(1)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)\+([0-9]+)-([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}+{m.group(2)}-{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(2)}-{m.group(3)}+{m.group(1)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)\+([0-9]+)\*([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}+{m.group(2)}*{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(1)}+{m.group(3)}*{m.group(2)}={m.group(4)}")
            commies.add(f"{m.group(2)}*{m.group(3)}+{m.group(1)}={m.group(4)}")
            commies.add(f"{m.group(3)}*{m.group(2)}+{m.group(1)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)\+([0-9]+)/([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}+{m.group(2)}/{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(2)}/{m.group(3)}+{m.group(1)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)-([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}-{m.group(2)}={m.group(3)}")
        elif m := re.search(r"^([0-9]+)-([0-9]+)\+([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}-{m.group(2)}+{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(1)}+{m.group(3)}-{m.group(2)}={m.group(4)}")
            commies.add(f"{m.group(3)}+{m.group(1)}-{m.group(2)}={m.group(4)}")
            commies.add(f"{m.group(3)}-{m.group(2)}+{m.group(1)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)-([0-9]+)-([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}-{m.group(2)}-{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(1)}-{m.group(3)}-{m.group(2)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)-([0-9]+)\*([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}-{m.group(2)}*{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(1)}-{m.group(3)}*{m.group(2)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)-([0-9]+)/([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}-{m.group(2)}/{m.group(3)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)\*([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}*{m.group(2)}={m.group(3)}")
            commies.add(f"{m.group(2)}*{m.group(1)}={m.group(3)}")
        elif m := re.search(r"^([0-9]+)\*([0-9]+)\+([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}*{m.group(2)}+{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(2)}*{m.group(1)}+{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(3)}+{m.group(1)}*{m.group(2)}={m.group(4)}")
            commies.add(f"{m.group(3)}+{m.group(2)}*{m.group(1)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)\*([0-9]+)-([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}*{m.group(2)}-{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(2)}*{m.group(1)}-{m.group(3)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)\*([0-9]+)\*([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}*{m.group(2)}*{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(1)}*{m.group(3)}*{m.group(2)}={m.group(4)}")
            commies.add(f"{m.group(2)}*{m.group(1)}*{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(2)}*{m.group(3)}*{m.group(1)}={m.group(4)}")
            commies.add(f"{m.group(3)}*{m.group(1)}*{m.group(2)}={m.group(4)}")
            commies.add(f"{m.group(3)}*{m.group(2)}*{m.group(1)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)\*([0-9]+)/([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}*{m.group(2)}/{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(2)}/{m.group(3)}*{m.group(1)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)/([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}/{m.group(2)}={m.group(3)}")
        elif m := re.search(r"^([0-9]+)/([0-9]+)\+([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}/{m.group(2)}+{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(3)}+{m.group(1)}/{m.group(2)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)/([0-9]+)-([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}/{m.group(2)}-{m.group(3)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)/([0-9]+)\*([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}/{m.group(2)}*{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(3)}*{m.group(1)}/{m.group(2)}={m.group(4)}")
        elif m := re.search(r"^([0-9]+)/([0-9]+)/([0-9]+)=([0-9]+)$", line):
            commies.add(f"{m.group(1)}/{m.group(2)}/{m.group(3)}={m.group(4)}")
            commies.add(f"{m.group(1)}/{m.group(3)}/{m.group(2)}={m.group(4)}")
        else:
            raise Exception("Unexpected equation syntax")
        done.update(commies)
        fout.write(f"{','.join(commies)}\n")

