#!/usr/bin/env python3
from array import array
from collections import defaultdict
from collections import Counter
from tqdm import tqdm
from fractions import Fraction
from hashlib import md5

symbols = ['-', '*', '/', '+', '=', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9']
equations_by_display = dict()

class Equation:
    # self.display: the display string
    # self.codes: the numerical encoding of the symbols in the display string
    # self.commie_id: a number associating this equation with all its commutative variations
    def __init__(self, display):
        self.display = display
        equations_by_display[self.display] = self
        self.codes = bytearray([0,0,0,0,0,0,0,0])
        for i,c in enumerate(self.display):
            self.codes[i] = symbols.index(c)
        self.commie_id = 0
        self.commies = list()

    def __str__(self):
        return self.display

def are_commies(equation_list):
    count = len(equation_list)
    if count < 2:
        return True
    if count > 6:
        return False
    first = equation_list[0].commie_id
    if equation_list[1].commie_id != first:
        return False
    if count > 2:
        for i in range(3, count):
            if equation_list[i] != first:
                return False
    return True

# -----------------------------------------------------------------------------------
# -----------------------------------------------------------------------------------
# -----------------------------------------------------------------------------------

print("Loading guesses.txt...")
all_guesses = list()
with open("guesses.sorted.txt", "r") as fin:
    for line in tqdm(fin, mininterval=1, leave=False, colour="green"):
        line = line.strip("\r\n")
        all_guesses.append(Equation(line))

print("Loading answers.txt...")
all_answers = list()
with open("answers.txt", "r") as fin:
    for line in tqdm(fin, mininterval=1, leave=False, colour="green"):
        line = line.strip("\r\n")
        all_answers.append(equations_by_display[line])

print("Loading commies.txt...")
equations_by_commie_id = defaultdict(list)
with open("commies.txt", "r") as fin:
    for equ_id, line in tqdm(enumerate(fin, start=1), mininterval=1, leave=False, colour="green"):
        line = line.rstrip("\r\n")
        for display in line.split(','):
            equ = equations_by_display[display]
            equ.commie_id = equ_id
            equations_by_commie_id[equ_id].append(equ)
            equ.commies = equations_by_commie_id[equ_id]

MASKS_HIGH = [1 << (8 + (7 - i)) for i in range(8)]
MASKS_LOW = [1 << (7 - i) for i in range(8)]

'''
Returns a hint encoded in a 16-bits integer
The higher 8 bits indicates the positions which are "green", correct symbol in correct position
The lower 8 bits indicates the positions which are "purple", correct symbol in wrong position
Any position not set in either the higher or lower bits indicates "black", no more such symbol in answer
'''
def calculate_hint(answer, guess):
    if answer.commie_id == guess.commie_id: return 0xFF00
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

def hint_to_string(guess, hint):
    s = ""
    for i in range(8):
        if hint & MASKS_HIGH[i] != 0:
            s += 'G'
        elif hint & MASKS_LOW[i] != 0:
            s += 'p'
        else:
            s += '.'
    return s

# -----------------------------------------------------------------------------------
# -----------------------------------------------------------------------------------
# -----------------------------------------------------------------------------------
class Node:
    #
    # self.parent: The parent Node
    # self.answer_list: The list (of Equation) of possible answers
    # self.guess_number: The number of this guess from the start of the game
    # self.guess: The guess
    # self.buckets: The dict (int to Node) of possible answers grouped by the resulting hints
    #
    # AGW = Average Guess count to Win
    # PWT = Probability of Win in Three or less guesses
    #
    # The node can be in one of 3 states:
    # 1) Has only an answer_list.
    #    In this state, we can only get approximate values for AGW and PWT.
    # 2) Has a guess and buckets, but the sub-nodes
    # 3) Has answer_list, guess and buckets
    #
    def __init__(self, parent):
        self.parent = parent
        self.answer_list = list()
        if parent is None:
            self.guess_number = 1
        else:
            self.guess_number = parent.guess_number + 1
        self.guess = None
        self.buckets = None
        self.agw = -1
        self.pwt = -1
        self.accurate = False

    def clone(self):
        clone = Node(None)
        clone.parent = self.parent
        clone.answer_list = self.answer_list
        clone.guess_number = self.guess_number
        clone.guess = self.guess
        clone.buckets = self.buckets
        clone.agw = self.agw
        clone.pwt = self.pwt
        clone.accurate = self.accurate
        return clone

    def become(self, other):
        self.parent = other.parent
        self.answer_list = other.answer_list
        self.guess_number = other.guess_number
        self.guess = other.guess
        self.buckets = other.buckets
        self.agw = other.agw
        self.pwt = other.pwt
        self.accurate = other.accurate

    def __str__(self):
        str = f"Node: {self.get_guess_chain()} with {len(self.answer_list)} answers"
        if self.buckets is not None:
            str += f" with {len(self.buckets)} buckets"
        str += f" agw {self.agw} -- pwt {self.pwt} -- {"not " if not self.accurate else ""}accurate"
        return str

    def print(self):
        print("----------------------------------------------------------")
        print(f"Node: {self.get_guess_chain()} with {len(self.answer_list)} answers")
        if self.buckets is None:
            print("No buckets")
        else:
            sorted_hints = sorted(self.buckets.keys(), key=lambda x: len(self.buckets[x].answer_list), reverse=True)
            for hint in sorted_hints:
                print(f"Hint {hint_to_string(self.guess, hint)} with {len(self.buckets[hint].answer_list)} answers")
        print(f"agw {self.agw} -- pwt {self.pwt} -- {"not " if not self.accurate else ""}accurate")
        print("----------------------------------------------------------")

    def build_buckets(self):
        self.buckets = dict()
        for answer in self.answer_list:
            hint = calculate_hint(answer, self.guess)
            node = self.buckets.get(hint)
            if node is None:
                node = Node(self)
                self.buckets[hint] = node
            node.answer_list.append(answer)

    def get_guess_chain(self):
        chain = ""
        if self.parent is not None:
            chain = self.parent.get_guess_chain() + " "
        if self.guess is None:
            chain += "????????"
        else:
            chain += self.guess.display
        return chain

    def get_answer_list_hash(self):
        h = md5()
        for equ in self.answer_list:
            h.update(equ.display.encode("ascii"))
        return h.hexdigest()

    def calculate_theoretical_best(self):
        commie_ids = Counter()
        for answer in self.answer_list:
            commie_ids[answer.commie_id] += 1
        most_common = max(commie_ids.values())
        self.agw = Fraction(len(self.answer_list) * 2 - most_common, len(self.answer_list))
        if self.guess_number >= 3:
            self.pwt = Fraction(0,1)
        elif self.guess_number == 2:
            if self.agw == 1:
                self.pwt = Fraction(1,1)
            else:
                self.pwt = Fraction(most_common, len(self.answer_list))
        elif self.guess_number == 1:
            self.pwt = Fraction(1,1)

    def evaluate(self):
        count = len(self.answer_list)
        if count == 1:
            self.guess = self.answer_list[0]
            self.agw = 1
            if self.guess_number <= 3:
                self.pwt = 1
            else:
                self.pwt = 0
            self.accurate = True
        if count <= 6 and are_commies(self.answer_list):
            # If all the answers are commutative equivalents, then any of them will win the game
            self.guess = self.answer_list[0]
            self.agw = 1
            if self.guess_number <= 3:
                self.pwt = 1
            else:
                self.pwt = 0
            self.accurate = True
        if count == 2:
            # If we have exactly two possible answers that are not commutative equivalents,
            # then it will take an average of 1.5 guesses to win.
            # Half the time, we'll choose the right answer on the first try.
            # The other half of the time, we'll need a second guess to win.
            self.guess = self.answer_list[0]
            self.agw = Fraction(3,2)
            if self.guess_number <= 2:
                self.pwt = 1
            elif self.guess_number == 3:
                self.pwt = Fraction(1,2)
            else:
                self.pwt = 0
            self.accurate = True
        if self.buckets is None:
            self.agw = 9
            self.pwt = 0
            self.accurate = False
        else:
            self.accurate = True
            agw_sum = 0
            pwt_sum = 0
            for hint, node in self.buckets.items():
                bucket_weight = Fraction(len(node.answer_list), len(self.answer_list))
                agw_sum += len(node.answer_list) * node.agw
                pwt_sum += len(node.answer_list) * node.pwt
                self.accurate &= node.accurate
            self.agw = agw_sum / len(self.answer_list)

    def enumerate_candidate_guesses(self):
        seen = set()
        for guess in self.answer_list:
            if guess not in seen:
                seen.add(guess)
                yield guess
        for answer in self.answer_list:
            for guess in answer.commies:
                if guess not in seen:
                    seen.add(guess)
                    yield guess
        for guess in all_guesses:
            if guess not in seen:
                seen.add(guess)
                yield guess

    def find_best_guess(self):
        self.evaluate()
        if self.guess is not None and self.accurate:
            return
        best = None
        theoretical_best = self.clone()
        theoretical_best.calculate_theoretical_best()
        for guess in self.enumerate_candidate_guesses():
            cand = self.clone()
            cand.guess = guess
            cand.build_buckets()
            cand.evaluate()
            if compare_nodes(cand, theoretical_best) == 0 and cand.accurate:
                best = cand
                break
            elif best is None:
                best = cand
            elif compare_nodes(cand, best) < 0:
                best = cand
        self.become(best)



def compare_nodes(a, b):
    if a.agw < b.agw:
        return -1
    if a.agw > b.agw:
        return 1
    if a.pwt < b.pwt:
        return -1
    if a.pwt > b.pwt:
        return 1
    return 0

# -----------------------------------------------------------------------------------
# -----------------------------------------------------------------------------------
# -----------------------------------------------------------------------------------

def bucket_top_nodes():
    print("Building top nodes...")
    firsts = list()
    for guess in tqdm(all_guesses, mininterval=1, leave=False, colour="green"):
        first = Node(None)
        first.answer_list = all_answers
        first.guess = guess
        firsts.append(first)

    print("Bucketing top nodes...")
    # firsts = firsts[:1000]
    hashes = set()
    with open("buckets.csv", "w") as fout:
        fout.write("Guess,Hint,AnswerListLength,AnswerListHash\n")
    #    for first in tqdm(firsts, mininterval=1, leave=False, colour="green"):
        guess_count = 0
        hash_count = 0
        for first in firsts:
            print(f"Processing {first.guess.display}, {guess_count} of {len(firsts)} with {hash_count} lists and {len(hashes)} unique lists...")
            guess_count += 1
            first.build_buckets()
            for hint,node in first.buckets.items():
                hash = node.get_answer_list_hash()
                fout.write(f"{first.guess.display},{hint_to_string(first.guess, hint)},{len(node.answer_list)},{hash}\n")
                hashes.add(hash)
                hash_count += 1
            first.answer_list = None
            first.buckets = None
    print(f"{len(hashes)} unique hashes.")

# -----------------------------------------------------------------------------------
# -----------------------------------------------------------------------------------
# -----------------------------------------------------------------------------------

top = Node(None)
top.answer_list = all_answers
top.guess = all_guesses[0]
top.find_best_guess()
top.print()
print("Done.")
