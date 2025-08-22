# TubeManager Essential Test Cases

This document outlines the primary test cases for the TubeManager script. The goal is to ensure robust and correct behavior for all main gameplay interactions, including stacking, matching, lid control, state cleanup, and edge conditions.

---

## 1. **Ball Stacking**

- **TC-01:** Ball dropped into empty tube is stacked in the bottom-most slot.
- **TC-02:** Multiple balls dropped into same tube stack up in sequential rows.
- **TC-03:** Ball stacking only occurs after the ball comes to rest (velocity below threshold for the required frames).
- **TC-04:** Ball dropped into a full tube is ignored (no stacking, no crash).

---

## 2. **Match-3 Detection & Clearing**

### Vertical
- **TC-10:** Stack three balls of the same color in the same column; they are cleared and score is awarded.
- **TC-11:** Stack three balls of different colors in the same column; no match, balls remain.

### Horizontal
- **TC-20:** Stack three balls of the same color in the same row (across three columns); they are cleared and score is awarded.
- **TC-21:** Stack three balls of different colors in the same row; no match, balls remain.

### Diagonal
- **TC-30:** Stack three balls of the same color diagonally (top-left to bottom-right or top-right to bottom-left); they are cleared and score is awarded.
- **TC-31:** Stack three balls of different colors diagonally; no match, balls remain.

---

## 3. **Lid Closing**

- **TC-40:** After stacking three balls (regardless of color) in a single column, the corresponding lid is closed.
- **TC-41:** Lid does not close for less than three balls in a column.
- **TC-42:** Lid remains closed after clearing balls from under it (does not auto-reopen).

---

## 4. **Game Over**

- **TC-50:** When all columns are full (all rows in every column are occupied), GameManager.Instance.GameOver() is triggered.
- **TC-51:** Game does not end if at least one slot in the grid is empty.

---

## 5. **Grid & State Cleanup**

- **TC-60:** Calling `ClearAllTubesAndBalls()` destroys all balls, clears the grid, and resets all manager state.
- **TC-61:** After cleanup, new balls can be stacked into empty tubes as normal.

---

## 6. **Miscellaneous/Edge Cases**

- **TC-70:** Ball is not stacked or matched more than once (no double stacking or double match clearing).
- **TC-71:** Balls at the edge between two tubes are handled in only one tube, not both.
- **TC-72:** Ball is not stacked while still moving (velocity above threshold).
- **TC-73:** Gizmos display correct lane, row, and overlap region visuals in Scene view.

---

## 7. **Performance & Frequency**

- **TC-80:** Overlap detection and stacking do not run every frame but respect the `overlapCheckInterval` (default 0.5s).
- **TC-81:** No memory leaks or buildup of state in `ballRestingFrames`, `stackedBalls`, or `stackingInProgress`.

---

## 8. **Integration**

- **TC-90:** Works with BallSpawner to spawn next ball only after stacking and clearing logic is complete (if desired).
- **TC-91:** LidsController receives lid close events exactly once per column when it fills.

---

**Note:**  
These test cases should be covered in both playmode (gameplay) tests and, where possible, in automated unit/integration tests. Visual/manual confirmation is required for Gizmos and for actual gameplay stacking/matching flows.