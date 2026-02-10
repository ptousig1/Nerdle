#!/usr/bin/env python3
import itertools
import re
from tqdm import tqdm

def is_equation_true(expr):
    try:
        equ = re.sub(r'\b0+(\d+)\b', r'\1', expr)
        equ = re.sub('=', '==', equ)
        return eval(equ)
    except ZeroDivisionError:
        return False

validRegex = re.compile("^(?:[-+]?[0-9]+[-+*/])*[-+]?[0-9]+=(?:[-+]?[0-9]+[-+*/])*[-+]?[0-9]+$")

symbols = ['-', '*', '/', '+', '=', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9']

with open("guesses.txt", "w") as fout:
    for each in tqdm(itertools.product(symbols, repeat=8), total=15 ** 8, mininterval=1, leave=False, colour="green"):
        equ = ''.join(each)
        if validRegex.search(equ) and is_equation_true(equ):
            fout.write(f"{equ}\n")
