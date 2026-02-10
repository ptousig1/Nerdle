#!/usr/bin/env python3
from tqdm import tqdm
from array import array
from collections import defaultdict

symbols = ['-', '*', '/', '+', '=', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9']
equations_by_display = dict()

class Equation:
    # self.display: the display string
    # self.codes: the numerical encoding of the symbols in the display string
    def __init__(self, str):
        self.display = str
        equations_by_display[self.display] = self
        self.codes = bytearray([0,0,0,0,0,0,0,0])
        for i,c in enumerate(self.display):
            self.codes[i] = symbols.index(c)
        self.commieId = 0

print("Loading answers.txt...")
answers = list()
with open("answers.txt") as fin:
    for line in fin:
        line = line.strip("\r\n")
        answers.append(Equation(line))

print("Loading guesses.txt...")
guesses = list()
with open("guesses.txt") as fin:
    for line in fin:
        line = line.strip("\r\n")
        guesses.append(Equation(line))

print("Loading commies.txt...")
with open("commies.txt", "r") as fin:
    for id, line in enumerate(fin, start=1):
        line = line.rstrip("\r\n")
        for str in line.split(','):
            equ = equations_by_display[str]
            equ.commieId = id

MASKS_HIGH = [1 << (8 + (7 - i)) for i in range(8)]
MASKS_LOW = [1 << (7 - i) for i in range(8)]

'''
Returns a hint encoded in a 16-bits integer
The higher 8 bits indicates the positions which are "green", correct symbol in correct position
The lower 8 bits indicates the positions which are "purple", correct symbol in wrong position
Any position not set in either the higher or lower bits indicates "black", no more such symbol in answer
'''
def calculate_hint(answer, guess):
    if answer.commieId == guess.commieId: return 0xFF00
    hint = 0
    counts = array('B', [0] * 15)  # 'B' means unsigned byte (0–255)
    for i in range(8):
        if answer.codes[i] == guess.codes[i]:
            hint |= MASKS_HIGH[i]
        else:
            counts[answer.codes[i]] += 1
    for i in range(8):
        if (answer.codes[i] != guess.codes[i]) and counts[guess.codes[i]] > 0:
            hint |= MASKS_LOW[i]
            counts[guess.codes[i]] -= 1
    return hint

def split_answers_by_hint(answers, guess):
    buckets = defaultdict(list)
    for answer in answers:
        hint = calculate_hint(answer, guess)
        buckets[hint].append(answer)
    return buckets

print("Counting buckets of guesses...")
bucket_counts = dict()
for guess in tqdm(guesses, mininterval=1, leave=False, colour="green"):
    buckets = split_answers_by_hint(answers, guess)
    bucket_counts[guess] = len(buckets)

print("Sorting guesses...")
sorted_guesses = sorted(guesses, key=lambda x: bucket_counts[x], reverse=True)
with open("guesses.sorted.txt", "w") as fout:
    for guess in sorted_guesses:
        fout.write(f"{guess.display}\n")
