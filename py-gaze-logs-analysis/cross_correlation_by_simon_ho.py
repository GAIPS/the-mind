"""
@Filipa: This code was taken from the following website at 01/09/2020:
https://github.com/sho-87/pyutils_sh/blob/master/pyutils_sh/gaze.py
Functions for calculating various gaze/eye-tracking related statistics.
"""

import numpy as np


def cross_correlation(person1, person2, framerate=25, constrain_seconds=2):
    """
    Calculate cross-correlation between two gaze signals.

    This function takes 2 lists/arrays of data, each containing an individual's 
    coded gaze data from an eye-tracker, and calculates the normalized max 
    cross-correlation value with its associated lag.
    
    Additionally, it will also return the cross-correlation value at 0 lag, as 
    well as the entire normalized array as a Python list.
 
    Negative lag value means person2 lagged behind person1 by x frames
    e.g.
    A = [0,1,1,1,0,0,0]
    B = [0,0,0,1,1,1,0]
    cross_correlation(A,B)

    Positive lag value means person1 lagged behind person2 by x frames
    e.g.
    A = [0,0,0,1,1,1,0]
    B = [0,1,1,1,0,0,0]
    cross_correlation(A,B)

    Parameters
    ----------
    person1 : ndarray or list
        1D array of person 1's gaze over time, coded as 0 = not looking, 
        1 = looking. The values represent whether the person was looking at a 
        target at a particular point in time.
    person2 : ndarray or list
        1D array of person 2's gaze over time, coded as 0 = not looking, 
        1 = looking. The values represent whether the person was looking at a 
        target at a particular point in time.
    framerate : int, optional
        The framerate (frames per second) of the eye-tracker.
    constrain_seconds : int, optional
        Number of seconds to constrain the cross-correlation values by. The 
        returned lags and cross-correlations will be centered around 0 lag by 
        this many seconds.

    Returns
    -------
    max_R : float
        Maximum (normalized) cross-correlation value.
    max_lag_adj : float
        Lag at which max cross-correlation occurs.
    zero_R : float
        Cross-correlation value at 0 lag.
    norm_array : list
        A list of all (normalized) cross-correlation values.
    """

    # convert lists to numpy arrays
    x = np.array(person1)
    y = np.array(person2)

    # calculate cross correlation values
    correlations = np.correlate(x, y, "full")

    # trim the cross-correlation values to a range (-lag_limits : +lag_limits)
    # trim AFTER cross correlation calculation to avoid 0 padding of signals
    # assumes x and y are equal length
    lag_limits = constrain_seconds * framerate
    trimmed_correlations = correlations[len(x) - 1 - lag_limits : len(x) + lag_limits]

    # normalize the cross-correlation values for ease of comparison between
    # subjects
    #norm_array = trimmed_correlations / (np.linalg.norm(x) * np.linalg.norm(y))
    
    #filipa's normalization
    if np.linalg.norm(x) != 0 and np.linalg.norm(y) != 0:
        norm_array = trimmed_correlations / (np.linalg.norm(x) * np.linalg.norm(y))
    elif np.linalg.norm(x) == 0:
        print(">>>>>>>>> CASE 1")
        norm_array = trimmed_correlations / np.linalg.norm(y)
    elif np.linalg.norm(y) == 0:
        print(">>>>>>>>> CASE 2")
        norm_array = trimmed_correlations / np.linalg.norm(x)
    else:
        print(">>>>>>>>> CASE 3")
        norm_array = trimmed_correlations
    #norm_array = np.absolute(trimmed_correlations)

    # get maximum normalized cross-correlation value
    max_R = max(norm_array)

    # get lag of max correlation value
    max_lag = np.argmax(norm_array)

    # trimmed array is now 2*(lag_limits)+1 elements long
    # adjust it so that lag 0 is a complete match
    max_lag_adj = max_lag - lag_limits

    # Get the normalized zero lag correlation value
    zero_R = norm_array[lag_limits]

    return float(max_R), float(max_lag_adj), float(zero_R), norm_array.tolist()