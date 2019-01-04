# Allows to set the X teminal title via "set-title"
# Plus: automatically sets the terminal title to:
#   the directory name and git branch and command that is being executed
# put at the end of .bashrc, seems to not cause any issues with bash-it

function set-title(){
  echo -en "\033]0;$*\a"
  export STEVE_CUSTOM_TITLE=1
}

function get-auto-title() {
  local title=`basename $PWD`
  if [ -d .git ]; then
    local title="$title:$(git rev-parse --abbrev-ref HEAD)"
  fi
  echo $title
}

function set-auto-title(){
  if [ -z "$var" ]; then
    echo -ne "\033]0;$(get-auto-title)\007"
  fi
}

function preexec_invoke_exec () {  
  if [ -z "$var" ]; then
    [ -n "$COMP_LINE" ] && return  # do nothing if completing
    [ "$BASH_COMMAND" = "$PROMPT_COMMAND" ] && return # don't cause a preexec for $PROMPT_COMMAND
    local this_command=`HISTTIMEFORMAT= history 1 | sed -e "s/^[ ]*[0-9]*[ ]*//"`;
    echo -ne "\033]0;$(get-auto-title)> ${this_command}\007"
  fi
}
trap 'preexec_invoke_exec' DEBUG
PROMPT_COMMAND="$PROMPT_COMMAND;set-auto-title"
